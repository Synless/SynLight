#include<FastLED.h>
#define NUM_LEDS 112
#define DATA_PIN 2

const int maxBrightness = 90;

CRGB leds[NUM_LEDS];

int red = 0;
int green = 0;
int blue = 0;

#include <ESP8266WiFi.h>
#include <DNSServer.h>
#include <ESP8266WebServer.h>
#include <WiFiManager.h>  //https://github.com/tzapu/WiFiManager
#include <WiFiUDP.h>

const unsigned int localPort = 8787;
WiFiUDP UDP;
boolean udpConnected = false;
char packetBuffer[UDP_TX_PACKET_MAX_SIZE];
char ReplyBuffer[] = "pong";

unsigned long t1 = 0;
unsigned int ledCounter = 0;
unsigned int totalLedCounter = 0;
bool active = true;

void setup()
{
    Serial.begin(115200);
    FastLED.addLeds<NEOPIXEL, DATA_PIN>(leds, NUM_LEDS);
    
    fill(0, NUM_LEDS, 20, 0, 0);

    //https://github.com/tzapu/WiFiManager#how-it-works
    WiFiManager wifiManager;
    delay(10);
    wifiManager.setSTAStaticIPConfig(IPAddress(192,168,8,175), IPAddress(192,168,8,1), IPAddress(255,255,255,0));
    wifiManager.autoConnect("SynLight");
    fill(0, NUM_LEDS, 15, 15, 0);
    delay(100);

    udpConnected = (UDP.begin(localPort) == 1);
    if (udpConnected)
    {
        fill(0, NUM_LEDS, 0, 20, 0);
        delay(300);
    }

    fill(0, NUM_LEDS, 0, 0, 0);
}

void loop()
{
    if (udpConnected)
    {
        int packetSize = UDP.parsePacket();
        if (packetSize)
        {
            UDP.read(packetBuffer, UDP_TX_PACKET_MAX_SIZE);
            t1 = millis();

            active = true;

            //Serial.print("Packet of size : ");Serial.println(packetSize);
            
            if(packetBuffer[0]==0)//PING
            {
                if(packetSize==5)
                {
                    if(packetBuffer[1]=='p' && packetBuffer[2]=='i' && packetBuffer[3]=='n' && packetBuffer[4]=='g')
                    {
                        Serial.print("Received ping command, answering [");Serial.print(ReplyBuffer);Serial.print("] to -> ");Serial.print(UDP.remoteIP());Serial.print(" on port ");Serial.println(localPort);

                        UDP.beginPacket(UDP.remoteIP(), localPort);
                        UDP.write(ReplyBuffer);
                        UDP.endPacket();
                    }
                }
            }
            else if(packetBuffer[0]==2 || packetBuffer[0]==3)
            {
                int ledNb = (packetSize-1)/3;
                Serial.print("Number of LEDs in this packet : "); Serial.println(ledNb);

                while(ledCounter < ledNb && ledCounter < NUM_LEDS)
                {
                    red   = packetBuffer[ledCounter*3 + 1];
                    green = packetBuffer[ledCounter*3 + 2];
                    blue  = packetBuffer[ledCounter*3 + 3];
                    
                    leds[ledCounter+totalLedCounter] = CRGB(red>>2,green>>2,(blue*3)>>4);                    

                    ledCounter++;
                }

                totalLedCounter += ledCounter;
                ledCounter = 0;

                if(packetBuffer[0]==3)
                {
                    FastLED.show();
                    totalLedCounter = 0;
                }   
            }
        }
    }

    //STANDBY AFTER 7 SECONDS
    if ((millis() - t1>5000) && active)
    {
        active = false;
        t1 = millis();
        fill(0, NUM_LEDS, 0, 0, 0);
    }
}
void fill(int _start, int _end, int r, int g, int b)
{
    for (int n = _start; n < NUM_LEDS && n < _end; n++) { leds[n] = CRGB(r,g,(b*3)>>2); }
    FastLED.show(); 
}

void setBrightness(int brightness) { FastLED.setBrightness(max(0,min(maxBrightness,brightness))); }