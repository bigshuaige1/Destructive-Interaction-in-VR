
float forceSensorValue;
double releasedStringLength;

double spoolMotorAngle = 0;
double brakeMotorAngle = 0;
double spoolMotorTargetAngle = 0;
double brakeMotorTargetAngle = 0;
double frictionMin = 0;
double frictionMax = 100;

const double spoolRadius = 1; //输入线轴半径
const double brakeMotorMinAngle = 10.0; //输入刹车电机最松角度
const double brakeMotorMaxAngle = -30.0; //输入刹车电机最紧角度

int frictionActive = 8;
float desiredForce = 0.0;
float handSpoolDistance = 0.0;

char *ins = NULL;
char orins[100]; // 存储接收到的串口数据

void setup() {
Serial.begin(115200);

}

void loop() {


 if (Serial.available()) {
        // 读取直到换行符，假设发送的每条数据以换行符结束
        int length = Serial.readBytesUntil('\n', orins, sizeof(orins) - 1);
        orins[length] = '\0'; // 确保字符串以空字符结尾

        // 使用 strtok 分割字符串
        ins = strtok(orins, ",");
        if (ins != NULL) {
            frictionActive = atoi(ins); // 转换为整数
            ins = strtok(NULL, ",");
            desiredForce = atof(ins); // 转换为浮点数
            ins = strtok(NULL, ",");
            handSpoolDistance = atof(ins); // 转换为浮点数

        }
 }

Serial.print(" frictionActive: ");
Serial.print(frictionActive);
Serial.print("desiredForce: ");
Serial.print(desiredForce, 4); // Four decimal places for precision
Serial.print(" handSpoolDistance: ");
Serial.println(handSpoolDistance, 4); // Four decimal places for precision





delay(100);
}



