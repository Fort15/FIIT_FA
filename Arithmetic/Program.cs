// тут можно что-то тестить

using Arithmetic.BigInt;

BetterBigInteger a = new("111111111111111111111111111111111111111111111111", 10);
BetterBigInteger b = new("1", 10);
BetterBigInteger sum = a + b;
BetterBigInteger product = a * b;

// Ожидаемый вывод: 123 + 321 = 444
Console.WriteLine($"{a} + {b} = {sum}"); 

// 123 * 321 = 39483
Console.WriteLine($"{a} * {b} = {product}"); 

// Тест вычитания
BetterBigInteger diff = a - b;
Console.WriteLine($"{a} - {b} = {diff}");

// Тест знака
BetterBigInteger neg = new BetterBigInteger("999", 10);
Console.WriteLine($"-{neg} = {-neg}");


