#include <HID-Project.h>
#include <HID-Settings.h>

// --------------- Part to adapt ---------------

#define BUTTON_PIN1  2
#define BUTTON_PIN2  3
#define BUTTON_PIN3  4
#define BUTTON_PIN4  5
#define BUTTON_PIN5  6
#define BUTTON_PIN6  7
#define BUTTON_PIN7  8
#define BUTTON_PIN8  9
#define BUTTON_PIN9  10
#define BUTTON_PIN10 14
#define BUTTON_PIN11 15
#define BUTTON_PIN12 16

// --------------- ^^^ Part to adapt ^^^ ---------------

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
  {BUTTON_PIN9, 8}, {BUTTON_PIN10, 9}, {BUTTON_PIN11, 10}, {BUTTON_PIN12, 11},
};
const uint8_t NumButtons = sizeof(buttons) / sizeof(Button);

uint8_t rawhidBuffer[64];
uint8_t lastReport[4] = {0, 0, 0, 0};

void setup() {
  Serial.begin(9600);

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

  uint8_t report[4] = {
    (uint8_t)(buttonMask & 0xFF),
    (uint8_t)((buttonMask >> 8) & 0x0F),
    0, // pas d'encodeur
    0
  };

  if(report[0] != lastReport[0] || report[1] != lastReport[1]){
    uint8_t fullReport[64] = {0};
    memcpy(fullReport, report, sizeof(report));
    RawHID.write(fullReport, sizeof(fullReport));
    memcpy(lastReport, report, sizeof(report));
  }

  // On vide les output reports du host (page/heure/notif) : rien à en faire, pas d'écran.
  if(RawHID.available()){
    uint8_t dump[64];
    RawHID.readBytes((char*)dump, sizeof(dump));
  }
}