#include <Wire.h>  
#include "I2C.h"

#define SERIAL_BAUD 115200 
#define M1MODULE  0x48 

// Global variable
char command_pc;

void setup() {
  // setting wire begin, but do not set address (maybe do not know address)
  Wire.begin();
  I2c.begin();
  // setting computer serial baud
  Serial.begin(SERIAL_BAUD);
  // Help note
//  Serial.println("Start this scanning program, please input a to start");
//  Serial.println("=====================================================");
}

void FindSlaveAddress() {
  // local variable
  uint8_t error, i2cAddress, devCount, unCount, index;

  // print i2c scanner starting
//  Serial.println("Already prepare I2C Scanner started......");
//  Serial.println("Scanning...");

  // scan program
  devCount = 0;
  unCount = 0;
  index = 1;
  for(i2cAddress = 1; i2cAddress < 127; i2cAddress++ ) {
    // master 向所以位置發出連線，並嘗試連線
    Wire.beginTransmission(i2cAddress);
    // feedback 連線結果
    error = Wire.endTransmission(); 
    if (error == 0) {
      Serial.print("Spo2 at I2C 0x");
      if (i2cAddress<16) Serial.print("0");
      Serial.println(i2cAddress,HEX);
      devCount++;
      index++;
    } else if (error==4) {
      Serial.print("Unknow error at 0x");
      if (i2cAddress<16) Serial.print("0");
      Serial.println(i2cAddress,HEX);
      unCount++;
    }    
  }

  if (devCount + unCount == 0) {
    Serial.println("No I2C devices found ");
  }
//  else {
//    Serial.print("-----> " + (String)devCount + " device(s) is/are found address");
//    if (unCount > 0) Serial.print(", and unknown error in " + (String)unCount + " address");
//  }
//  Serial.println(" ..."); Serial.println();
//  delay(5000);
}

uint8_t ReadRegisterAddress(uint8_t addressSlave, uint8_t addressRegister, bool isIndicate){
  uint8_t valueRegister;
  I2c.read(addressSlave,addressRegister,1);
  valueRegister = I2c.receive() << 1;
  if (isIndicate) {
    /*
    Serial.print("Register Address 0x");
    if (addressRegister<16) Serial.print("0");
    Serial.print(addressRegister,HEX);
    Serial.print("  ==> Register Value:"); 
    Serial.print(valueRegister,BIN);
    Serial.println(".");
     */
    Serial.print("==> 0x");
    if (addressRegister<16) Serial.print("0");
    Serial.print(addressRegister,HEX);
    Serial.print(":");
    Serial.print(valueRegister,BIN);
    Serial.println(".");
  }
  return valueRegister;
}

void WriteRegisterAddress(uint8_t addressSlave, uint8_t addressRegister, uint8_t valueRegister, bool isIndicate){
  I2c.write(addressSlave,addressRegister,valueRegister);
  if (isIndicate) {
    /*    
    Serial.print("Register Address 0x");
    if (addressRegister<16) Serial.print("0");
    Serial.print(addressRegister,HEX);
    Serial.print("  <== Register Value "); 
    Serial.print(valueRegister,BIN);
    Serial.println("");
    */
    Serial.print("<== 0x");
    if (addressRegister<16) Serial.print("0");
    Serial.print(addressRegister,HEX);
    Serial.print(":");
    Serial.print(valueRegister,BIN);
    Serial.println("");
    
  }
}

String SerialReadLine(){ 
  String s = "";
  do {
    char c = Serial.read();
    if(c!='\n') s += c;
    delay(5);    // 沒有延遲的話 UART 串口速度會跟不上Arduino的速度，會導致資料不完整
  } while (Serial.available());
  return s; 
}

void loop()
{
  if(Serial.available()>0){
    String stringInput = SerialReadLine();
    int stringlength = stringInput.length()-1;
    int intInput = stringInput.toInt();
    switch (intInput) {
        case 0:
          ReadRegisterAddress(M1MODULE,0x0F,true);
          ReadRegisterAddress(M1MODULE,0x0F,true);
          break;
        case 1:
          FindSlaveAddress(); 
          break;
        case 2:
          ReadRegisterAddress(M1MODULE,0x03,true);
          ReadRegisterAddress(M1MODULE,0x04,true);
          break;
        case 3:
          WriteRegisterAddress(M1MODULE,0x03,0xFF,true);
          ReadRegisterAddress(M1MODULE,0x03,true);
          break;
        case 4:
          WriteRegisterAddress(M1MODULE,0x00,0x01,true);
          ReadRegisterAddress(M1MODULE,0x00,true);
          break;
        case 5:
          WriteRegisterAddress(M1MODULE,0x00,0x00,true);
          ReadRegisterAddress(M1MODULE,0x00,true);
          break;
        case 6:
          ReadRegisterAddress(M1MODULE,0x02,true);
          break;
        case 7:
          ReadRegisterAddress(M1MODULE,0x01,true);        
          break;
        case 8:
          ReadRegisterAddress(M1MODULE,0x0E,true);
          break;
        default:
          break;
    }
    // Serial.println(stringlength);
    // int inputvalue = stringInput.toInt();
    // Serial.println(inputvalue,BIN);
  }
}
