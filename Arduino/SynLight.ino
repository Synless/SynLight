#include <NeoPixelBus.h>  //https://github.com/Makuna/NeoPixelBus
#include <ESP8266WiFi.h>
#include <DNSServer.h>
#include <ESP8266WebServer.h>
#include <WiFiManager.h>  //https://github.com/tzapu/WiFiManager
#include <WiFiUDP.h>

// Depending on the setup, has to be at least the number of LEDs
const uint16_t PixelCount = 1000; 
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
int ledCounter = 1;

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
    delay(500);
    Serial.begin(115200);
    strip.Begin();
    strip.Show();
    delay(100);
    fill(0, PixelCount, 20, 0, 0);
    delay(100);
    //https://github.com/tzapu/WiFiManager#how-it-works
    WiFiManager wifiManager;
    delay(100);
    wifiManager.autoConnect("SynLight");
    fill(0, PixelCount, 15, 15, 0);
    delay(100);
    udpConnected = (UDP.begin(localPort) == 1);
    if (udpConnected)
    {
        fill(0, PixelCount, 0, 20, 0);
        delay(100);
    }    
}

void loop()
{
    if (udpConnected)
    {
        int packetSize = UDP.parsePacket();        
        if (packetSize)
        {
            t1 = millis();

            Serial.print("Packet of size : ");Serial.println(packetSize);            
            
            UDP.read(packetBuffer, UDP_TX_PACKET_MAX_SIZE);

            if(packetBuffer[0]==0)      //PING
            {
                Serial.println("Received : Ping header");
                if(packetSize==5)
                {
                    if(packetBuffer[1]=='p' && packetBuffer[2]=='i' && packetBuffer[3]=='n' && packetBuffer[4]=='g')
                    {
                        Serial.println("Received : Ping command")
                        Serial.print("Answering -> [");Serial.print(ReplyBuffer);Serial.println("]");

                        UDP.beginPacket(UDP.remoteIP(), localPort);
                        UDP.write(ReplyBuffer);
                        UDP.endPacket();
                    }
                }
            }
            else if(packetBuffer[0]==1) //STATIC
            {
                Serial.println("Received : Static color header");
                if(packetSize==2)
                {
                    Serial.println("Received : Static color command"); 
                    fill(0, PixelCount, packetBuffer[1], packetBuffer[1], packetBuffer[1]);                                                        
                }
            }
            else if(packetBuffer[0]==2 || packetBuffer[0]==3)
            {
                Serial.println("Received : Payload header");
                if(packetSize>3)
                {
                    Serial.println("Received : Payload command"); 
                    while(ledCounter<packetSize - 2 && ledCounter<packetSize - 2)
                    {
                        red = packetBuffer[ledCounter];
                        green = packetBuffer[ledCounter + 1];
                        blue = packetBuffer[ledCounter + 2];                        
                        if((ledCounter/3)<PixelCount)      
                        {
                            //POWERED FROM A SINGLE USB3.0 CONNECTION, NO EXTERNAL PSU, THUS THE DIVISIONS
                            strip.SetPixelColor(ledCounter/3,RgbColor(red>>2,green>>2,(blue*3)>>4));   
                        }
                        else
                        {
                            break;
                        }
                        ledCounter += 3;                        
                    }                   
                    if(packetBuffer[0]==3)
                    {
                        strip.Show();
                        ledCounter = 1;
                        Serial.println("--- Show ---"); 
                    }                                                         
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
