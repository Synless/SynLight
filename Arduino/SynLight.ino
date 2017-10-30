#include <NeoPixelBus.h>  //https://github.com/Makuna/NeoPixelBus
#include <ESP8266WiFi.h>
#include <DNSServer.h>
#include <ESP8266WebServer.h>
#include <WiFiManager.h>  //https://github.com/tzapu/WiFiManager
#include <WiFiUDP.h>

// Depending on the setup, has to be at least the number of LEDs
const uint16_t PixelCount = 100; 
const int PixelPin = 2;
NeoPixelBus<NeoGrbFeature, Neo800KbpsMethod> strip(PixelCount, PixelPin);
//WIFI
const unsigned int localPort = 8787;
WiFiUDP UDP;
boolean udpConnected = false;
char packetBuffer[UDP_TX_PACKET_MAX_SIZE];
char ReplyBuffer[] = "pong";

int red = 0;
int green = 0;
int blue = 0;
unsigned long t1 = 0;
bool timer = true;

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
    Serial.begin(115200);
    strip.Begin();
    strip.Show();
    delay(800);
    fill(0, PixelCount, 20, 0, 0);
    delay(100);
    //https://github.com/tzapu/WiFiManager#how-it-works
    WiFiManager wifiManager;
    wifiManager.autoConnect("SynLight");
    fill(0, PixelCount, 15, 15, 0);
    delay(100);
    udpConnected = (UDP.begin(localPort) == 1);
    if (udpConnected)
    {
        fill(0, PixelCount, 0, 20, 0);
        delay(200);
    }    
}

void loop()
{
    if (udpConnected)
    {
        int packetSize = UDP.parsePacket();        
        if (packetSize)
        {
            Serial.print("Packet of size ");
            Serial.println(packetSize);
            
            UDP.read(packetBuffer, UDP_TX_PACKET_MAX_SIZE);
            if (packetSize == 4)
            {   //PINGING PACKET
                t1 = millis();
                if(packetBuffer[0]=='p' && packetBuffer[1]=='i' && packetBuffer[2]=='n' && packetBuffer[3]=='g')
                {
                    UDP.beginPacket(UDP.remoteIP(), localPort);
                    UDP.write(ReplyBuffer);
                    UDP.endPacket();
                    Serial.print("Ping received, answered [");Serial.print(ReplyBuffer);Serial.println("]");                    
                }
                //STATIC COLOR
                else if(packetBuffer[0]==1)
                {
                    fill(0, PixelCount, packetBuffer[1], packetBuffer[2], packetBuffer[3]);
                    Serial.print("Static color");
                }
            }
            //FRAME
            else if (packetSize>1)
            {
                t1 = millis();
                for (int n = 0; n<packetSize - 2; n += 3)
                {
                    red = packetBuffer[n];
                    green = packetBuffer[n + 1];
                    blue = packetBuffer[n + 2];
                    //POWERED FROM A SINGLE USB3.0 CONNECTION, NO EXTERNAL PSU, THUS THE DIVISIONS
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
            //SPECIAL PACKETS
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
    //STANDBY AFTER 7 SECONDS
    if (millis() - t1>7000)
    {
      t1 = millis();
      fill(0, PixelCount, 0, 0, 0);
    }
}
