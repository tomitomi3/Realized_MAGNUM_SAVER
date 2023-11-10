//-------------------------------------------------------------------
// ミニN駆 モーター制御
//-------------------------------------------------------------------
#include <WiFi.h>
#include <WiFiUdp.h>
#include <M5Unified.h>

const char *ssid = "[SSID]";
const char *password = "[PASSWORD]";

// PWM
const int freq = 1000;    // PWM周波数
const int resolution = 8; // PWM分解能 8bit

const int pwmChG26 = 0;
const int PWM_CH = 26;
const int LED_PIN = 10;

// ネットワーク
WiFiUDP udp;
unsigned int localUdpPort = 12345;
bool isWifiConnected = false;

// 現在のPWM制御 レベル
int currentDutyLevel = 0;

// 時刻で段階的に加速が減少 デッドマンスイッチ
unsigned long changeMotorSpeedTime = 0; // 前回のデューティー更新時刻
const int COUNTDONW_SEC = 4000;

// debug コメントでリリース向け
// #define DEBUG

// --------------------------------------
// プロトタイプ宣言
// --------------------------------------

void SetMotorSpeedUsingPWM(char motorStepAscii);
void DisplayScreen();
void OneshotLED();

// --------------------------------------
// setup, loop
// --------------------------------------
void setup()
{
  M5.begin();

  // --------------
  // LCDセットアップ
  // --------------
  M5.Lcd.fillScreen(TFT_BLACK);
  M5.Lcd.setRotation(1);
  M5.Lcd.setCursor(0, 0);
  M5.Lcd.setTextColor(TFT_WHITE);

  // --------------
  // GPIO制御
  // --------------
  // 出力設定
  pinMode(LED_PIN, OUTPUT);
  pinMode(PWM_CH, OUTPUT);

  // 内臓LEDをOFF
  digitalWrite(LED_PIN, HIGH);

  // PWM設定
  ledcSetup(pwmChG26, freq, resolution);
  ledcAttachPin(PWM_CH, pwmChG26);
  ledcWrite(pwmChG26, 0);

  // --------------
  // wifi
  // --------------
  // wifi接続
  WiFi.begin(ssid, password);
  int tryConnectCount = 0;
  while (WiFi.status() != WL_CONNECTED)
  {
    delay(200);
    M5.Lcd.print(".");

    if (++tryConnectCount >= 50)
    {
      M5.Lcd.fillScreen(TFT_RED);
      M5.Lcd.setCursor(0, 0);
      M5.Lcd.setTextColor(TFT_WHITE);
      M5.Lcd.println("Error WiFi");
      delay(2000);
      DisplayScreen(); // ウイング表示
      return;
    }
  }
  isWifiConnected = true;

  // Wi-Fi接続成功時の処理
  M5.Lcd.fillScreen(TFT_BLACK); // 画面クリア
  M5.Lcd.setCursor(0, 0);
  M5.Lcd.println("Connected!");
  M5.Lcd.print("IP: ");
  M5.Lcd.println(WiFi.localIP().toString());

  // udp開始
  udp.begin(localUdpPort);

  // 表示して消す
  delay(3000);
  DisplayScreen(); // ウイング表示

  // 省エネ
  // LCD明るさ、クロック周波数を下げる
  M5.Lcd.setBrightness(128);
  setCpuFrequencyMhz(80); // wifi利用可能下限
}

void loop()
{
#ifdef DEBUG
  {
    // GetVinVoltage()を使用して電圧を取得
    float vinVoltage = M5.Power.Axp192.getACINVoltage();

    // ディスプレイの内容をクリア
    M5.Lcd.fillScreen(BLACK);

    // 電圧をディスプレイに表示
    M5.Lcd.setCursor(0, 0);
    M5.Lcd.printf("Vin Voltage: %.2f V", vinVoltage);
  }
#endif

  // リセットボタン
  if (M5.BtnA.wasPressed())
  {
    ESP.restart();
  }

  // WiFiが接続されていない場合、何もしない
  if (!isWifiConnected)
  {
    return;
  }

  // UDPパケット受信
  int rcvPacketSize = udp.parsePacket();
  if (rcvPacketSize > 0)
  {
    // 1バイト受信
    char receivedData = 0;
    udp.read(&receivedData, 1);

    // 動作モードを決定
    SetMotorSpeedUsingPWM(receivedData);

    // バッファクリア
    if (rcvPacketSize > 1)
    {
      char tempBuffer[64];
      while (udp.available())
      {
        udp.read(tempBuffer, sizeof(tempBuffer));
      }
    }
  }

  // 一定間隔で減速
  unsigned long nowTime = millis();
  if (nowTime - changeMotorSpeedTime >= COUNTDONW_SEC)
  {
    changeMotorSpeedTime = nowTime;
    if (currentDutyLevel-- > 0)
    {
      SetMotorSpeedUsingPWM('0' + currentDutyLevel);
    }
  }

  M5.update();
  delay(50);
}

// --------------------------------------
// 関数
// --------------------------------------

/// @brief スクリーン表示 ウィング表示
void DisplayScreen()
{
  // 背景
  uint16_t bgColor = M5.Lcd.color565(0, 0, 64);
  M5.Lcd.fillScreen(bgColor);

  M5.Lcd.setTextColor(TFT_YELLOW);
  M5.Lcd.setTextSize(3);

  String text1 = "MAGNAUM";
  String text2 = "Saver";

  // センタリング
  int textWidth1 = M5.Lcd.textWidth(text1);
  int textWidth2 = M5.Lcd.textWidth(text2);
  int textHeight = M5.Lcd.fontHeight();

  int cursorX1 = (M5.Lcd.width() - textWidth1) / 2;
  int cursorX2 = (M5.Lcd.width() - textWidth2) / 2;

  int totalHeight = 2 * textHeight;
  int cursorY1 = (M5.Lcd.height() - totalHeight) / 2;
  int cursorY2 = cursorY1 + textHeight;

  M5.Lcd.setCursor(cursorX1, cursorY1);
  M5.Lcd.println(text1);
  M5.Lcd.setCursor(cursorX2, cursorY2);
  M5.Lcd.println(text2);
}

/// @brief モーター制御
/// @param motorStepAscii モーター制御ステップ 0～9 Asciiで来る
void SetMotorSpeedUsingPWM(char motorStepAscii)
{
  // asciiコードで1から9の文字列の時にモーターをPWM制御
  if (motorStepAscii > '0' && motorStepAscii <= '9')
  {
    // 数値に変換
    currentDutyLevel = motorStepAscii - '0';

    // 1～9を80から255の範囲に割り当てる
    int duty = map(currentDutyLevel, 1, 9, 80, 255);
    ledcWrite(pwmChG26, duty);

    OneshotLED();
  }
  else
  {
    currentDutyLevel = 0;
    ledcWrite(pwmChG26, 0); // PWM OFF

    OneshotLED();
    OneshotLED();
  }

  changeMotorSpeedTime = millis();
}

/// @brief 内臓LEDをON
void OneshotLED()
{
  // M5StickC 内臓LED ON
  digitalWrite(LED_PIN, LOW);
  delay(70);
  digitalWrite(LED_PIN, HIGH);
  delay(70);
}