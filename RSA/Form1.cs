using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
namespace RSA
{
    public partial class Form1 : Form
    {
        private uint n = 0; // Модуль системы RSA (n = p * q)
        private Random rand = new Random();

        public Form1()
        {
            InitializeComponent();
        }

        // 1. Быстрое возведение в степень по модулю (рекурсивно): (m^e) mod n
        private uint ModPow(uint m, uint e, uint n)
        {
            if (n == 1) return 0;
            if (e == 0) return 1;

            ulong half = ModPow(m, e / 2, n);
            ulong result = (half * half) % n;

            if (e % 2 == 1)
            {
                result = (result * (m % n)) % n;
            }

            return (uint)result;
        }

        // 2. Тест Ферма на простоту (проверяем 20 разных случайных чисел b)
        private bool IsPrimeFermat(uint p)
        {
            if (p < 2) return false;
            if (p == 2 || p == 3) return true;
            if (p % 2 == 0) return false;

            for (int i = 0; i < 20; i++)
            {
                uint b = (uint)rand.Next(2, (int)p - 1);
                if (ModPow(b, p - 1, p) != 1)
                {
                    return false; // Составное число
                }
            }
            return true; // Простое число
        }

        // Генерация простого числа в интервале до 65536 (2^16)
        private uint GeneratePrime()
        {
            while (true)
            {
                uint candidate = (uint)rand.Next(256, 65536);
                if (IsPrimeFermat(candidate))
                {
                    return candidate;
                }
            }
        }

        // 3. Алгоритм Евклида: наибольший общий делитель gcd(a, b)
        private uint Gcd(uint a, uint b)
        {
            while (b != 0)
            {
                uint temp = b;
                b = a % b;
                a = temp;
            }
            return a;
        }

        // 4. Расширенный алгоритм Евклида
        private long ExtendedEuclidean(long a, long b, out long mu, out long eta)
        {
            if (b == 0)
            {
                mu = 1;
                eta = 0;
                return a;
            }

            long gcd = ExtendedEuclidean(b, a % b, out long mu1, out long eta1);
            mu = eta1;
            eta = mu1 - (a / b) * eta1;
            return gcd;
        }

        // КНОПКА 1 "Сгенерировать ключи"
        private void button1_Click(object sender, EventArgs eArgs)
        {
            //Выбираем два случайных простых числа p и q
            uint p = GeneratePrime();
            uint q = GeneratePrime();
            while (q == p)
            {
                q = GeneratePrime();
            }

            //Вычисляем модуль n = p * q
            n = p * q;

            //Вычисляем функцию Эйлера phi(n) = (p - 1) * (q - 1)
            uint phi = (p - 1) * (q - 1);

            //Выбираем открытый ключ e < phi, взаимно простой с phi: gcd(e, phi) == 1
            uint eKey;
            do
            {
                eKey = (uint)rand.Next(3, (int)Math.Min(phi, 65536));
            } while (Gcd(eKey, phi) != 1);

            //Вычисляем закрытый ключ d через расширенный алгоритм Евклида
            ExtendedEuclidean(eKey, phi, out long mu, out long eta);

            long dKey = mu % phi;
            if (dKey < 0)
            {
                dKey += phi;
            }

            // Выводим ключи 
            textBox3.Text = eKey.ToString();
            textBox4.Text = dKey.ToString();
            textBox1.Text = n.ToString();
            p = 0;
            q = 0;
            phi = 0;

            MessageBox.Show("Ключи успешно сгенерированы!");
        }

        // КНОПКА 2 "Зашифровать"
        private void button2_Click(object sender, EventArgs eArgs)
        {
            if (n == 0 || !uint.TryParse(textBox3.Text, out uint eKey))
            {
                MessageBox.Show("Сначала нажмите кнопку «Сгенерировать ключи»!");
                return;
            }

            string plainText = textBox5.Text;
            if (string.IsNullOrEmpty(plainText))
            {
                MessageBox.Show("Введите исходный текст в окно «Открытый текст»!");
                return;
            }

            Encoding encoding = Encoding.GetEncoding(1251);
            byte[] bytes = encoding.GetBytes(plainText);
            List<string> encryptedNumbers = new List<string>();

            foreach (byte m in bytes)
            {
                uint c = ModPow(m, eKey, n);
                encryptedNumbers.Add(c.ToString());
            }

            textBox6.Text = string.Join(" ", encryptedNumbers);
        }

        // КНОПКА 3 "Расшифровать"
        private void button3_Click(object sender, EventArgs eArgs)
        {
            if (n == 0 || !uint.TryParse(textBox4.Text, out uint dKey))
            {
                MessageBox.Show("Нет закрытого ключа d или модуля n!");
                return;
            }

            string cipherText = textBox6.Text.Trim();
            if (string.IsNullOrEmpty(cipherText))
            {
                MessageBox.Show("Окно «Зашифрованный текст» пусто!");
                return;
            }

            string[] parts = cipherText.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            List<byte> decryptedBytes = new List<byte>();

            foreach (string part in parts)
            {
                if (uint.TryParse(part, out uint c))
                {
                    uint m = ModPow(c, dKey, n);
                    decryptedBytes.Add((byte)m);
                }
            }


            Encoding encoding = Encoding.GetEncoding(1251);
            textBox5.Text = encoding.GetString(decryptedBytes.ToArray());
        }

        // ====================================================================
        // Вспомогательный метод для чтения текста из файла
        private string LoadTextFromFile()
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*";
                openFileDialog.Title = "Выберите файл для загрузки";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        // Читаем файл в кодировке Windows-1251
                        return File.ReadAllText(openFileDialog.FileName, Encoding.GetEncoding(1251));
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Ошибка при чтении файла: " + ex.Message);
                    }
                }
            }
            return null;
        }

        // Вспомогательный метод для сохранения текста в файл
        private void SaveTextToFile(string content, string defaultFileName)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                MessageBox.Show("Поле пустое, сохранять нечего!");
                return;
            }

            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*";
                saveFileDialog.Title = "Сохранить результат в файл";
                saveFileDialog.FileName = defaultFileName;

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        // Сохраняем файл в кодировке Windows-1251
                        File.WriteAllText(saveFileDialog.FileName, content, Encoding.GetEncoding(1251));
                        MessageBox.Show("Файл успешно сохранен!");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Ошибка при сохранении файла: " + ex.Message);
                    }
                }
            }
        }

        // КНОПКА "Загрузить открытый текст" (button5)
        private void button5_Click(object sender, EventArgs e)
        {
            string loadedText = LoadTextFromFile();
            if (loadedText != null)
            {
                textBox5.Text = loadedText;
            }
        }

        // КНОПКА "Сохранить открытый текст" (button6)
        private void button6_Click(object sender, EventArgs e)
        {
            SaveTextToFile(textBox5.Text, "plain_text.txt");
        }

        // КНОПКА "Загрузить шифротекст" (button7)
        private void button7_Click(object sender, EventArgs e)
        {
            string loadedCipher = LoadTextFromFile();
            if (loadedCipher != null)
            {
                textBox6.Text = loadedCipher;
            }
        }

        // КНОПКА "Сохранить шифротекст" (button8)
        private void button8_Click(object sender, EventArgs e)
        {
            SaveTextToFile(textBox6.Text, "cipher_text.txt");
        }
    }
    }
