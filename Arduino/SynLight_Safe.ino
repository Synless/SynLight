#include "FastLED.h"
#define NUM_LEDS 520 // x:16 AND y:10 ON THE REAR OF THE SCREEN CORRESPONDS TO x:18 AND y:12 ON THE PC SOFTWARE
#define DATA_PIN 8
CRGB leds[NUM_LEDS];

#include <ESP8266WiFi.h>
#include <WiFiUDP.h>
const char* ssid = "Synless_Wifi";
const char* password = "--------";
const unsigned int localPort = 8787; //CONST ?
WiFiUDP UDP;
boolean udpConnected = false;
char packetBuffer[UDP_TX_PACKET_MAX_SIZE];
char ReplyBuffer[] = "a0";

int red = 0;
int green = 0;
int blue = 0;
unsigned long t1 = 0;
bool timer = true;

//DEBUGING PURPOSE, TALKING BACK THROUGH THE SERIAL TERMINAL
#define debug 1

boolean connectWifi()
{
  boolean state = true;
  int i = 1;
  WiFi.begin(ssid, password);
#if debug
  Serial.println(WiFi.macAddress());
  Serial.print("\nConnecting to ");
  Serial.print(ssid);
  Serial.print(" using password ");
  Serial.println(password);
#endif
  while (WiFi.status() != WL_CONNECTED) //20 CONNECTION ATTEMPS
  {
#if debug
    Serial.println(i);
#endif
    delay(500);
    if (i++ >= 20) { state = false; break; }
  }
#if debug
  if (state)
  {
    Serial.print("\nConnected to ");
    Serial.println(ssid);
    Serial.print("IP address : ");
    Serial.println(WiFi.localIP());
  }
  else
  {
    Serial.println("\nConnection failed. Check the SSID and the password");
  }
#endif
  return state;
}

void fill(int start, int end, int r, int g, int b)
{
  for (int n = start; n < NUM_LEDS && n < end; n++) { leds[n] = CRGB(r, g, b); }
  FastLED.show();
}

void setup()
{
#if debug
  Serial.begin(115200);
#endif
  FastLED.addLeds<NEOPIXEL, DATA_PIN>(leds, NUM_LEDS); //NEOPIXEL, WS2812B HAVE CHANNELS SWAPED, ADAFRUIT ?? 
  delay(800);
  fill(0, NUM_LEDS, 10, 0, 0);
  delay(100);
  if (connectWifi())
  {
    fill(0, NUM_LEDS, 8, 8, 0);
    delay(100);
    udpConnected = (UDP.begin(localPort) == 1);
    if (udpConnected)
    {
      fill(0, NUM_LEDS, 0, 10, 0);
      delay(100);
    }
  }
}

void loop()
{
  if (udpConnected)
  {
    int packetSize = UDP.parsePacket();
    if (packetSize)
    {
      UDP.read(packetBuffer, UDP_TX_PACKET_MAX_SIZE);
      if (packetSize == 4)
      {
        if( packetBuffer[0]=='p' && packetBuffer[1]=='i' && packetBuffer[2]=='n' && packetBuffer[3]=='g')
        {
#if debug
            Serial.print("Ping received\nSending back "); 
            Serial.print(ReplyBuffer); 
            Serial.print(" to ");
            Serial.print(UDP.remoteIP());
            Serial.print(" on ");
            Serial.println(localPort);
#endif
            UDP.beginPacket(UDP.remoteIP(), localPort);
            UDP.write(ReplyBuffer);
            UDP.endPacket();
        }
      }
      else if (packetSize>1)
      {
#if debug
        Serial.print("Received packet of size ");
        Serial.print(packetSize);
        Serial.print(" from ");
        Serial.println(UDP.remoteIP());
#endif
        t1 = millis();
        for (int n = 0; n<packetSize - 2; n += 3)
        {
          red = packetBuffer[n];
          green = packetBuffer[n + 1];
          blue = packetBuffer[n + 2];
          //POWERED FROM A SINGLE USB3.0 CONNECTION
          leds[n / 3] = CRGB(red >> 2, green >> 2, (blue*3) >> 4); //DIMMING BLUE
        }
        FastLED.show();
      }
      else if (packetSize == 1)
      {
        t1 = millis();
        UDP.read(packetBuffer, UDP_TX_PACKET_MAX_SIZE);
        int specialPacket = packetBuffer[0];
#if debug
        Serial.print("Received special packet "); Serial.println(specialPacket);
#endif
        switch (specialPacket) 
        {
          case 0:
            break;
          case 1:
            fill(0, NUM_LEDS, 25, 25, 25);
            break;
          case 2:
            fill(0, NUM_LEDS, 0, 0, 0);
            break;
          default:
            break;
        }  
      }
    }
  }
  if (millis() - t1>7000)
  {
    t1 = millis();
    fill(0, NUM_LEDS, 0, 0, 0);
  }
}
