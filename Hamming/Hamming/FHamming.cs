using System;
using System.IO;
using System.Windows.Forms;

namespace Hamming
{
    public partial class FHamming : Form
    {//матрица кодирования
        private readonly int[] _hammingCodeMatrix = {1, 2, 4, 8, 14, 13, 11, 7};
        //{1,0,0,0,0,1,1,1}
        //{0,1,0,0,1,0,1,1}
        //{0,0,1,0,1,1,0,1}
        //{0,0,0,1,1,1,1,0}
        //матрица для получения синдромов
        private readonly int[] _hammingDecodeMatrix = { 30, 45, 75, 135 };
        //{0,1,1,1}  
        //{1,0,1,1}   
        //{1,1,0,1}    
        //{1,1,1,0}    
        //{1,0,0,0}    
        //{0,1,0,0}   
        //{0,0,1,0}
        //{0,0,0,1}
        //матрица для обнаружения ошибок
        private readonly int[] _errorCodes = {7, 11, 13, 14, 8, 4, 2, 1};
        //{1,1,1,0,0,0,0,1}  
        //{1,1,0,1,0,0,1,0}  
        //{1,0,1,1,0,1,0,0}  
        //{0,1,1,1,1,0,0,0}  

        private bool _isDoubleErrorFound;
        private readonly byte[] _codeTable = new byte[16];


        public FHamming()
        {
            InitializeComponent();
        }

        private void CodeBtn_Click(object sender, EventArgs e)
        {

            var ofd = new OpenFileDialog();
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                OutTextBox.AppendText("Кодирование...\n\n");
                using (var stream = new FileStream(ofd.FileName, FileMode.Open)) // открываем на чтение
                using (var br = new BinaryReader(stream))
                {
                    using (var sw = new FileStream(ofd.FileName + "ham", FileMode.Create)) // отркываем на запись
                    using (var bw = new BinaryWriter(sw))
                    {
                        for (int i = 0; i < br.BaseStream.Length; i++)
                        {
                            var b = br.ReadByte();
                            bw.Write(_codeTable[b >> 4]); //кодируем 4 бита 8ю сначала первые 4
                            bw.Write(_codeTable[b & 15]); // а потом - вторые
                        }
                        OutTextBox.AppendText("Фаил успешно закодирован\n");
                    }
                }


            }

        }

        private void DecodeBtn_Click(object sender, EventArgs e)
        {
            var newpath = string.Empty;
            var ofd = new OpenFileDialog();
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                OutTextBox.AppendText("Декодирование...\n\n");
                using (var stream = new FileStream(ofd.FileName, FileMode.Open))
                using (var br = new BinaryReader(stream))
                {
                    newpath = ofd.FileName.Substring(0, ofd.FileName.Length - 3);
                    using (var sw = new FileStream(newpath, FileMode.Create))
                    using (var bw = new BinaryWriter(sw))
                    {
                        _isDoubleErrorFound = false;

                        for (int i = 0; i < br.BaseStream.Length; i+=2)
                        {
                            var b1 = br.ReadByte(); // декодируем 2 байта в 1
                            var b2 = br.ReadByte();
                            var outByte = (DecodeByte(b1)<<4) | DecodeByte(b2);
                            if (_isDoubleErrorFound) // если двойная ошибка - все в топку
                                break;
                            
                            bw.Write((byte)outByte);
                        }

                        OutTextBox.AppendText(_isDoubleErrorFound ? "Обнаруженна двойная ошибка - невозможно декодировать фаил\n" : "Фаил успешно декодирован\n");
                    }
                }

                if (_isDoubleErrorFound) File.Delete(newpath);
                


            }

        }
        // кодируем 4 бита в 8
        private int CodeByte(int b)
        {
            var output = 0;
           
            for (int i = 0; i < 8; i++)
            {
                var s = b & _hammingCodeMatrix[i];
                var k = s&1;
                for (int j = 1; j < 4; j++)
                {
                    s >>= 1;
                    k ^= s & 1;
                }
                output |= k;
                output <<= 1;
            }

            return output>>1;

        }
        // декодируем
        private int DecodeByte(int b)
        {
            var s = GetSyndrome(b);
            if (s != 0)
            {
                var index = Array.IndexOf(_errorCodes, s); // если вектор синдромов не 0 преверяем есть ли он в массиве
                if (index == -1) //нет - две ошибки
                {
                    OutTextBox.AppendText("Обнаружена двойная ошибка!\n");
                    _isDoubleErrorFound = true;
                }
                else // есть - одна. исправляем
                {
                    OutTextBox.AppendText("Обнаружена одиночная ошибка - исправлено\n");
                    b ^= (1 << index);
                }
            }
            
            var output = 0;

            for (int i = 4; i < 8; i++)
            {
                output |= ((b >> i) & 1);
                output <<= 1;
            } // получаем из вектора закодированные 4 бита

            return output >> 1;
        }
        
     
        private int GetSyndrome(int b)
        {
            int output = 0; // получаем вектор синдромов

            for (int i = 0; i < 4; i++)
            {
                var s = b & _hammingDecodeMatrix[i];
                var k = s & 1;
                for (int j = 1; j < 8; j++)
                {
                    s >>= 1;
                    k ^= s & 1;
                }
                output |= k;
                output <<= 1;
            }
            output >>= 1;
            return output;
        }


        private void Form1_Load(object sender, EventArgs e)
        {// при загрузке строим массив всех возможных кодов. Потом что бы закодировать - просто берем значение по индексу
            for (byte i = 0; i < 16; i++)
            {
                _codeTable[i] = (byte)CodeByte(i);
            }
        }

      
    }
}
