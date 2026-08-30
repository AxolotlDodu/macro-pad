#include <HID-Project.h>
#include <HID-Settings.h>
#include <Wire.h>
#include <Adafruit_GFX.h>
#include <Adafruit_SH110X.h>

// --- Écran OLED SH1106 1.3" 128x64 (I2C) ---
#define SCREEN_WIDTH 128
#define SCREEN_HEIGHT 64
#define OLED_RESET -1        // pas de pin reset dédiée (partagée avec l'Arduino)
#define SCREEN_I2C_ADDR 0x3C // 0x3C le plus courant, 0x3D sur certains modules

Adafruit_SH1106G display(SCREEN_WIDTH, SCREEN_HEIGHT, &Wire, OLED_RESET);

// ============================================================
// PIN DEFINITIONS
// ============================================================

// --- 10 switchs ---
#define BUTTON_PIN1 0
#define BUTTON_PIN2 1
#define BUTTON_PIN3 4
#define BUTTON_PIN4 5
#define BUTTON_PIN5 6
#define BUTTON_PIN6 7
#define BUTTON_PIN7 8
#define BUTTON_PIN8 9
#define BUTTON_PIN9 10
#define BUTTON_PIN10 14

// --- Encodeur 1 : A0 (A), A1 (B), 15 (bouton) ---
#define ENC1_PIN_A A0
#define ENC1_PIN_B A1
#define ENC1_PIN_BTN 15

// --- Encodeur 2 : A2 (A), A3 (B), 16 (bouton) ---
#define ENC2_PIN_A A2
#define ENC2_PIN_B A3
#define ENC2_PIN_BTN 16

// --- Écran I2C : pins 2 (SDA) et 3 (SCL) natifs du Leonardo ---
// (Wire.begin() suffit, pas besoin de définir SDA/SCL manuellement)

const uint8_t ledPin = 17;

// ============================================================
// CLASSE BOUTON (switchs + boutons d'encodeurs)
// ============================================================
class Button {
  public:
  const uint8_t pin;
  const uint8_t bitIndex;

  Button(uint8_t p, uint8_t idx) : pin(p), bitIndex(idx) {}

  boolean update(){
    boolean state = !digitalRead(pin);
    if(state == pressed || (millis() - lastPressed <= debounceTime)){
      return pressed;
    }
    lastPressed = millis();
    pressed = state;
    return pressed;
  }

  private:
  const unsigned long debounceTime = 30;
  unsigned long lastPressed = 0;
  boolean pressed = 0;
};

Button buttons[] = {
  {BUTTON_PIN1, 0}, {BUTTON_PIN2, 1}, {BUTTON_PIN3, 2}, {BUTTON_PIN4, 3},
  {BUTTON_PIN5, 4}, {BUTTON_PIN6, 5}, {BUTTON_PIN7, 6}, {BUTTON_PIN8, 7},
  {BUTTON_PIN9, 8}, {BUTTON_PIN10, 9},
};
const uint8_t NumButtons = sizeof(buttons) / sizeof(Button);

// ============================================================
// CLASSE ENCODEUR ROTATIF (quadrature, polling, sans interruption
// car A0-A3/15/16 ne sont pas des pins PCINT/EXTINT sur le 32U4)
// ============================================================
class Encoder {
  public:
  const uint8_t pinA, pinB;

  Encoder(uint8_t a, uint8_t b) : pinA(a), pinB(b) {}

  void begin(){
    lastState = (digitalRead(pinA) << 1) | digitalRead(pinB);
  }

  // Met à jour l'état interne, accumule le delta (+1/-1 par cran)
  void update(){
    uint8_t s = (digitalRead(pinA) << 1) | digitalRead(pinB);
    if(s != lastState){
      static const int8_t table[16] = {
         0, 1,-1, 0,
        -1, 0, 0, 1,
         1, 0, 0,-1,
         0,-1, 1, 0
      };
      uint8_t idx = (lastState << 2) | s;
      delta += table[idx];
      lastState = s;
    }
  }

  // Retourne le delta accumulé et le remet à 0
  int8_t consumeDelta(){
    int8_t d = delta;
    delta = 0;
    return d;
  }

  private:
  uint8_t lastState = 0;
  int8_t delta = 0;
};

Encoder encoder1(ENC1_PIN_A, ENC1_PIN_B);
Encoder encoder2(ENC2_PIN_A, ENC2_PIN_B);
Button  encBtn1(ENC1_PIN_BTN, 10); // bit 10 -> bouton encodeur 1
Button  encBtn2(ENC2_PIN_BTN, 11); // bit 11 -> bouton encodeur 2

// ============================================================
// RAPPORT HID
//   byte0 : switchs bits 0-7
//   byte1 : bit0-1 = switchs bits 8-9, bit2 = bouton enc1,
//           bit3 = bouton enc2, bits4-7 = réservés
//   byte2 : delta encodeur 1 (int8, cumulé depuis dernier envoi)
//   byte3 : delta encodeur 2 (int8, cumulé depuis dernier envoi)
// ============================================================
uint8_t rawhidBuffer[64];
uint8_t lastReport[4] = {0, 0, 0, 0};

void setup() {
  Serial.begin(9600);

  // --- Failsafe au boot ---
  // Ancienne version : lecture de la pin 1 seule (désormais utilisée
  // par un switch). Remplacé par une combinaison : les deux boutons
  // d'encodeur maintenus enfoncés au démarrage.
  pinMode(ENC1_PIN_BTN, INPUT_PULLUP);
  pinMode(ENC2_PIN_BTN, INPUT_PULLUP);
  if(!digitalRead(ENC1_PIN_BTN) && !digitalRead(ENC2_PIN_BTN)){
    failsafe();
  }

  pinMode(ledPin, OUTPUT);
  digitalWrite(ledPin, HIGH);
  TXLED0;

  for(int i = 0; i < NumButtons; i++){
    pinMode(buttons[i].pin, INPUT_PULLUP);
  }

  pinMode(ENC1_PIN_A, INPUT_PULLUP);
  pinMode(ENC1_PIN_B, INPUT_PULLUP);
  pinMode(ENC2_PIN_A, INPUT_PULLUP);
  pinMode(ENC2_PIN_B, INPUT_PULLUP);
  encoder1.begin();
  encoder2.begin();

  Wire.begin(); // I2C pour l'écran (pins 2/3 natives du Leonardo)

  if(!display.begin(SCREEN_I2C_ADDR, true)){
    Serial.println("Écran SH1106 non détecté !");
  } else {
    display.clearDisplay();
    display.setTextSize(1);
    display.setTextColor(SH110X_WHITE);
    display.setCursor(0, 0);
    display.println("Macro Pad OK");
    display.display();
  }

  RawHID.begin(rawhidBuffer, sizeof(rawhidBuffer));
}

void loop() {
  uint16_t buttonMask = 0;

  for(int i = 0; i < NumButtons; i++){
    if(buttons[i].update()){
      buttonMask |= (1 << buttons[i].bitIndex);
    }
  }
  if(encBtn1.update()) buttonMask |= (1 << encBtn1.bitIndex);
  if(encBtn2.update()) buttonMask |= (1 << encBtn2.bitIndex);

  encoder1.update();
  encoder2.update();

  uint8_t report[4] = {
    (uint8_t)(buttonMask & 0xFF),
    (uint8_t)((buttonMask >> 8) & 0x0F),
    0, // rempli seulement s'il y a un delta à envoyer (voir plus bas)
    0
  };

  // On ne "consomme" les deltas des encodeurs que si un envoi va avoir lieu
  boolean buttonsChanged = (report[0] != lastReport[0]) || (report[1] != lastReport[1]);
  boolean hasEncoderMovement = false;

  // Aperçu sans consommer, pour savoir si ça vaut le coup d'envoyer
  // (on relit juste le flag interne via une copie temporaire)
  int8_t d1 = encoder1.consumeDelta();
  int8_t d2 = encoder2.consumeDelta();
  if(d1 != 0 || d2 != 0) hasEncoderMovement = true;

  report[2] = (uint8_t)d1;
  report[3] = (uint8_t)d2;

  if(buttonsChanged || hasEncoderMovement){
    Serial.print("Envoi rapport, mask=");
    Serial.print(buttonMask, BIN);
    Serial.print(" enc1=");
    Serial.print(d1);
    Serial.print(" enc2=");
    Serial.println(d2);

    uint8_t fullReport[64] = {0};
    memcpy(fullReport, report, sizeof(report));
    RawHID.write(fullReport, sizeof(fullReport));

    memcpy(lastReport, report, sizeof(report));
  }
}

void failsafe(){
  for(;;){}
}
