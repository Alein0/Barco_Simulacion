// Pines de los potenciómetros
const int PIN_VELOCIDAD = A0;
const int PIN_TIMON = A1;

// Pines de botones
const int PIN_DISPARO = 2;
const int PIN_ANCLA = 3;

bool anclaBajada = false;

void setup() {
  Serial.begin(9600);

  pinMode(PIN_DISPARO, INPUT_PULLUP);
  pinMode(PIN_ANCLA, INPUT_PULLUP);
}

void loop() {
  // ===== POTENCIÓMETROS =====
  int valorVelocidad = analogRead(PIN_VELOCIDAD);
  int valorTimon = analogRead(PIN_TIMON);

  Serial.print(valorVelocidad);
  Serial.print(",");
  Serial.println(valorTimon);

  // ===== BOTÓN DISPARO =====
  if (digitalRead(PIN_DISPARO) == LOW) {
    Serial.println("DISPARO");
    delay(200); // evita spam
  }

  // ===== BOTÓN ANCLA =====
  if (digitalRead(PIN_ANCLA) == LOW) {
    anclaBajada = !anclaBajada;

    if (anclaBajada) {
      Serial.println("ANCLA_BAJA");
    } else {
      Serial.println("ANCLA_SUBE");
    }

    delay(200); // evita múltiples cambios
  }

  delay(20);
}