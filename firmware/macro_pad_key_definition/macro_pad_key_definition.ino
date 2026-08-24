#include <HID-Project.h>
#include <HID-Settings.h>

// Pin definitions (à définir)
#define BUTTON_PIN1 5
#define BUTTON_PIN2 4
#define BUTTON_PIN3 3
#define BUTTON_PIN4 2
#define BUTTON_PIN5 9
#define BUTTON_PIN6 8
#define BUTTON_PIN7 7
#define BUTTON_PIN8 6
#define BUTTON_PIN9 16
#define BUTTON_PIN10 15
#define BUTTON_PIN11 14
#define BUTTON_PIN12 10

// ---------------------------------

#include "HID-Project.h"

class button {
  public:
  const uint8_t pin;
  const uint8_t bitIndex;

  button(uint8_t p, uint8_t idx) : pin(p), bitIndex(idx) {}

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

button buttons[] = {
  {BUTTON_PIN1, 0}, {BUTTON_PIN2, 1}, {BUTTON_PIN3, 2}, {BUTTON_PIN4, 3},
  {BUTTON_PIN5, 4}, {BUTTON_PIN6, 5}, {BUTTON_PIN7, 6}, {BUTTON_PIN8, 7},
  {BUTTON_PIN9, 8}, {BUTTON_PIN10, 9}, {BUTTON_PIN11, 10}, {BUTTON_PIN12, 11},
};

const uint8_t NumButtons = sizeof(buttons) / sizeof(button);
const uint8_t ledPin = 17;

uint8_t rawhidBuffer[64];
uint8_t lastReport[2] = {0};

void setup() {
  Serial.begin(9600);
  pinMode(1, INPUT_PULLUP);
  if(!digitalRead(1)){
    failsafe();
  }

  pinMode(ledPin, OUTPUT);
  digitalWrite(ledPin, HIGH);
  TXLED0;

  for(int i = 0; i < NumButtons; i++){
    pinMode(buttons[i].pin, INPUT_PULLUP);
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

  uint8_t report[2] = {
    (uint8_t)(buttonMask & 0xFF), (uint8_t)(buttonMask >> 8)
  };

  if(memcmp(report, lastReport, sizeof(report)) != 0){
    Serial.print("Envoi rapport, mask=");
    Serial.println(buttonMask, BIN);

    uint8_t fullReport[64] = {0};
    fullReport[0] = report[0];
    fullReport[1] = report[1];
    RawHID.write(fullReport, sizeof(fullReport));

    memcpy(lastReport, report, sizeof(report));
  }
}

void failsafe(){
  for(;;){}
}