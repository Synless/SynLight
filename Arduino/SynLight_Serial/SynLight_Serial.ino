#include<FastLED.h>
#define NUM_LEDS 112
#define DATA_PIN 2

const int maxBrightness = 90;

CRGB leds[NUM_LEDS];
byte buffer[NUM_LEDS*3];

int red = 0;
int green = 0;
int blue = 0;

void setup()
{
    Serial.begin(115200);
    FastLED.addLeds<NEOPIXEL, DATA_PIN>(leds, NUM_LEDS);
    setBrightness(maxBrightness);
}

long t1 = 0;
long t2 = 0;
long t3 = 0;
int br = 0;
bool found;
bool start_fade = false;
bool serial_found = false;

void loop()
{
    t1 = millis();

    Serial.readBytes(buffer, sizeof(buffer));
    Serial.read();
    
    if(!serial_found && buffer[0] == 'p' && buffer[1] == 'i' && buffer[2] == 'n' && buffer[3] == 'g')        
        Serial.println("pong");
            
    found = false;

    for(int n = 0; n < NUM_LEDS; n++)
    {
        red   = buffer[n*3];
        green = buffer[(n*3) + 1];
        blue  = buffer[(n*3) + 2];
        leds[n] = CRGB(red,green,(blue*3)>>2);
        
        buffer[n*3] = 0;
        buffer[(n*3) + 1] = 0;
        buffer[(n*3) + 2] = 0;
        
        if(red+green+blue != 0)
        {
            found = true;
            start_fade = false;
        }
    }

    
    if(t1 - t2 > 500)
    {
        t2 = millis();        
        if(!found) { start_fade = true; }
    }
    if(t1 - t3 > 200)
    {
        t3 = millis();
        if(found)
        {
            br = max(0,min(maxBrightness,br+12));
            setBrightness(br);
        }
        else if(start_fade)
        {
            br = max(0,min(maxBrightness,br-10));
            setBrightness(br);
        }
    }

    FastLED.show();
}

void fill(int _start, int _end, int r, int g, int b)
{
    for (int n = _start; n < NUM_LEDS && n < _end; n++) { leds[n] = CRGB(r,g,(b*3)>>2); }
    FastLED.show(); 
}

void setBrightness(int brightness) { FastLED.setBrightness(max(0,min(maxBrightness,brightness))); }
