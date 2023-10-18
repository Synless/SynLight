#include<FastLED.h>
#define NUM_LEDS 112
#define DATA_PIN 2

const int maxBrightness = 80;

CRGB leds[NUM_LEDS];
byte buffer[NUM_LEDS*3];

int red = 0;
int green = 0;
int blue = 0;


void fill(int _start, int _end, int r, int g, int b)
{
    for (int n = _start; n < NUM_LEDS && n < _end; n++) { leds[n] = CRGB(r,g,(b*3)>>2); }
    FastLED.show(); 
}

void setup()
{
    Serial.begin(115200);
    FastLED.addLeds<NEOPIXEL, DATA_PIN>(leds, NUM_LEDS);
    setBrightness(maxBrightness);
    fill(0,NUM_LEDS,0,100,0);
    delay(250);
    fill(0,NUM_LEDS,0,0,0);
    delay(250);
    fill(0,NUM_LEDS,0,100,0);
    delay(250);
    fill(0,NUM_LEDS,0,0,0);
}

long i;
long t1 = 0;
long t2 = 0;
long t3 = 0;
int br = 0;
bool found;
bool start_fade = false;

void loop()
{
    t1 = millis();

    Serial.readBytes(buffer, sizeof(buffer));
    Serial.read();

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

        i = red+green+blue;
        if(i!=0)
        {
            found = true;
            start_fade = false;
        }
    }

    
    if(t1 - t2 > 500)
    {
        Serial.println("500");
        t2 = millis();
        if(!found)
        {
            start_fade = true;
        }
    }
    if(t1 - t3 > 200)
    {
        Serial.println("200");
        t3 = millis();
        if(found)
        {
            Serial.println("found");
            br = max(0,min(maxBrightness,br+12));
            setBrightness(br);
        }
        else if(start_fade)
        {
            Serial.println("start_fade");
            br = max(0,min(maxBrightness,br-10));
            setBrightness(br);
        }
    }

    FastLED.show();
}

void setBrightness(int brightness)
{
    FastLED.setBrightness(max(0,min(maxBrightness,brightness)));
    Serial.print("brightness : ");
    Serial.println(brightness);
    Serial.print("caped_brightness : ");
    Serial.println(max(0,min(maxBrightness,brightness)));
}
