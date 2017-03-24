#include <NeoPixelBus.h>
#include <ESP8266WiFi.h>
#include <WiFiUDP.h>

const uint16_t PixelCount = 52; // Depending on the setup, carefull not to overflow
const uint8_t PixelPin = 2;  // Make sure to set this to the correct pin, ignored for Esp8266

NeoPixelBus<NeoGrbFeature, Neo800KbpsMethod> strip(PixelCount, PixelPin);

const char* ssid = "Synless_Wifi"; //Your wifi and password
const char* password = "--------";
const unsigned int localPort = 8787;
WiFiUDP UDP;
boolean udpConnected = false;
char packetBuffer[UDP_TX_PACKET_MAX_SIZE];
char ReplyBuffer[] = "a0";

int red = 0;
int green = 0;
int blue = 0;
unsigned long t1 = 0;
bool timer = true;

boolean connectWifi()
{
    boolean state = true;
    int i = 1;
    WiFi.begin(ssid, password);
    while (WiFi.status() != WL_CONNECTED)
    {
        delay(500);
        if (i++ >= 20) 
        { 
            state = false;
            break; 
        }
    }
    return state;
}

void fill(int _start, int _end, int r, int g, int b)
{
    for (int n = _start; n < PixelCount && n < _end; n++) 
    { 
        strip.SetPixelColor(n,RgbColor(r,g,b));
    }
    strip.Show();
}

void setup()
{
    strip.Begin();
    strip.Show();
    delay(800);
    fill(0, PixelCount, 10, 0, 0);
    delay(100);
    if (connectWifi())
    {
        fill(0, PixelCount, 8, 8, 0);
        delay(100);
        udpConnected = (UDP.begin(localPort) == 1);
        if (udpConnected)
        {
            fill(0, PixelCount, 0, 10, 0);
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
                    UDP.beginPacket(UDP.remoteIP(), localPort);
                    UDP.write(ReplyBuffer);
                    UDP.endPacket();
                }
            }
            else if (packetSize>1)
            {
                t1 = millis();
                for (int n = 0; n<packetSize - 2; n += 3)
                {
                    red = packetBuffer[n];
                    green = packetBuffer[n + 1];
                    blue = packetBuffer[n + 2];
                    //POWERED FROM A SINGLE USB3.0 CONNECTION
                    if((n/3)<PixelCount)      
                    {
                        strip.SetPixelColor(n/3,RgbColor(red>>2,green>>2,(blue*3)>>4));   
                    }
                    else
                    {
                        break;
                    }
                }
                strip.Show();
            }
            else if (packetSize == 1)
            {
                t1 = millis();
                UDP.read(packetBuffer, UDP_TX_PACKET_MAX_SIZE);
                int specialPacket = packetBuffer[0];
                switch (specialPacket) 
                {
                  case 0:
                    break;
                  case 1:
                    fill(0, PixelCount, 25, 25, 25);
                    break;
                  case 2:
                    fill(0, PixelCount, 0, 0, 0);
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
      fill(0, PixelCount, 0, 0, 0);
    }
}
