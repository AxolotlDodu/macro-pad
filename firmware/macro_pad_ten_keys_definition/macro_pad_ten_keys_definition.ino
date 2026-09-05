#include <HID-Project.h>
#include <HID-Settings.h>
#include <Wire.h>
#include <Adafruit_GFX.h>
#include <Adafruit_SH110X.h>

#define SCREEN_WIDTH 128
#define SCREEN_HEIGHT 64
#define OLED_RESET -1
#define SCREEN_I2C_ADDR 0x3C

Adafruit_SH1106G display(SCREEN_WIDTH, SCREEN_HEIGHT, &Wire, OLED_RESET);

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

#define ENC1_PIN_A A0
#define ENC1_PIN_B A1
#define ENC1_PIN_BTN 15

#define ENC2_PIN_A A2
#define ENC2_PIN_B A3
#define ENC2_PIN_BTN 16

const uint8_t ledPin = 17;

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

class Encoder {
  public:
  const uint8_t pinA, pinB;

  Encoder(uint8_t a, uint8_t b) : pinA(a), pinB(b) {}

  void begin(){
    lastState = (digitalRead(pinA) << 1) | digitalRead(pinB);
  }

  void update(){
    uint8_t s = (digitalRead(pinA) << 1) | digitalRead(pinB);
    unsigned long now = millis();

    if(s != lastState){
      if(now - lastChangeTime < debounceTime) return; // rebond ignoré
      lastChangeTime = now;

      static const int8_t table[16] = {
         0, 1,-1, 0,
        -1, 0, 0, 1,
         1, 0, 0,-1,
         0,-1, 1, 0
      };
      uint8_t idx = (lastState << 2) | s;
      accumulator += table[idx];
      lastState = s;

      // "3" = repos (les deux pins au repos, pull-up). Un cran complet = 4 transitions valides
      // depuis le dernier repos. On ne remonte le delta qu'une fois de retour au repos,
      // ce qui élimine tout rebond partiel en cours de cran.
      if(s == 3){
        if(accumulator >= 4) delta += 1;
        else if(accumulator <= -4) delta -= 1;
        accumulator = 0;
      }
    }
  }

  int8_t consumeDelta(){
    int8_t d = delta;
    delta = 0;
    return d;
  }

  private:
  uint8_t lastState = 0;
  int8_t accumulator = 0;
  int8_t delta = 0;
  unsigned long lastChangeTime = 0;
  const unsigned long debounceTime = 2; // ms, anti-rebond entre transitions
};

Encoder encoder1(ENC1_PIN_A, ENC1_PIN_B);
Encoder encoder2(ENC2_PIN_A, ENC2_PIN_B);
Button  encBtn1(ENC1_PIN_BTN, 10);
Button  encBtn2(ENC2_PIN_BTN, 11);

uint8_t rawhidBuffer[64];
uint8_t lastReport[4] = {0, 0, 0, 0};

char currentPageName[21] = "";
uint8_t currentPageIndex = 0;
uint8_t currentPageTotal = 0;
uint8_t clockHour = 0;
uint8_t clockMinute = 0;
bool clockSet = false;

bool notificationActive = false;
unsigned long notificationStart = 0;
const unsigned long notificationDuration = 2000; // ms
char notificationText[21] = "";

void renderDisplay(){
  display.clearDisplay();

  if(notificationActive){
    display.setTextSize(1);
    display.setTextColor(SH110X_WHITE);

    char line1[21];
    char line2[21] = "";
    strncpy(line1, notificationText, sizeof(line1));
    line1[sizeof(line1) - 1] = '\0';

    char* sep = strchr(line1, ':');
    if(sep != nullptr){
      *sep = '\0';
      char* rest = sep + 1;
      while(*rest == ' ') rest++; // saute l'espace après ":"
      strncpy(line2, rest, sizeof(line2));
      line2[sizeof(line2) - 1] = '\0';
    }

    int16_t x1, y1;
    uint16_t w1, h1, w2, h2;
    display.getTextBounds(line1, 0, 0, &x1, &y1, &w1, &h1);

    if(strlen(line2) > 0){
      display.getTextBounds(line2, 0, 0, &x1, &y1, &w2, &h2);

      int16_t lineHeight = h1 + 4;
      int16_t totalHeight = lineHeight * 2;
      int16_t startY = (display.height() - totalHeight) / 2;

      display.setCursor((display.width() - (int16_t)w1) / 2, startY);
      display.print(line1);

      display.setCursor((display.width() - (int16_t)w2) / 2, startY + lineHeight);
      display.print(line2);
    } else {
      display.setCursor((display.width() - (int16_t)w1) / 2, (display.height() - (int16_t)h1) / 2);
      display.print(line1);
    }

    display.display();
    return;
  }

  display.setTextSize(1);
  display.setTextColor(SH110X_WHITE);
  display.setCursor(0, 0);
  if(clockSet){
    char timeStr[6];
    snprintf(timeStr, sizeof(timeStr), "%02d:%02d", clockHour, clockMinute);
    display.print(timeStr);
  } else {
    display.print("--:--");
  }

  char pageStr[8];
  snprintf(pageStr, sizeof(pageStr), "%d/%d", currentPageIndex, currentPageTotal);
  int16_t x1, y1;
  uint16_t w, h;
  display.getTextBounds(pageStr, 0, 0, &x1, &y1, &w, &h);
  display.setCursor(display.width() - (int16_t)w, 0);
  display.print(pageStr);

  display.setTextSize(2);
  display.getTextBounds(currentPageName, 0, 0, &x1, &y1, &w, &h);
  display.setCursor((display.width() - (int16_t)w) / 2, (display.height() - (int16_t)h) / 2);
  display.print(currentPageName);

  display.display();
}

void handleOutputReport(uint8_t* data, uint8_t len){
  if(len == 0) return;

  if(data[0] == 0x01 && len >= 4){
    currentPageIndex = data[1];
    currentPageTotal = data[2];
    uint8_t nameLen = data[3];
    if(nameLen > 20) nameLen = 20;
    if(len < (uint8_t)(4 + nameLen)) nameLen = len - 4;
    memcpy(currentPageName, &data[4], nameLen);
    currentPageName[nameLen] = '\0';
    renderDisplay();
  } else if(data[0] == 0x02 && len >= 3){
    clockHour = data[1];
    clockMinute = data[2];
    clockSet = true;
    renderDisplay();
  } else if(data[0] == 0x03 && len >= 2){
    uint8_t textLen = data[1];
    if(textLen > 20) textLen = 20;
    if(len < (uint8_t)(2 + textLen)) textLen = len - 2;
    memcpy(notificationText, &data[2], textLen);
    notificationText[textLen] = '\0';
    notificationActive = true;
    notificationStart = millis();
    renderDisplay();
  }
}

void setup() {
  Serial.begin(9600);

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

  Wire.begin();

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
    0,
    0
  };

  boolean buttonsChanged = (report[0] != lastReport[0]) || (report[1] != lastReport[1]);
  boolean hasEncoderMovement = false;

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

  if(RawHID.available()){
    uint8_t buffer[64];
    int count = RawHID.readBytes((char*)buffer, sizeof(buffer));
    if(count > 0){
      handleOutputReport(buffer, (uint8_t)count);
    }
  }
  if(notificationActive && (millis() - notificationStart >= notificationDuration)){
    notificationActive = false;
    renderDisplay();
  }
}

void failsafe(){
  for(;;){}
}
