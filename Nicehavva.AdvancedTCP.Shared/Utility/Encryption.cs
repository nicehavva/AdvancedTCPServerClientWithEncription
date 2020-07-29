using Nicehavva.AdvancedTCP.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nicehavva.AdvancedTCP.Shared.Utility
{
    class Encryption
    {
        public double[] choas_number_bin;
        public double[] choas_number_oct;
        public byte[] encrypt_information;
        public Encryption(int length,double First_X, double U, ChoasEnum choas_select)
        {
            choas_creation(length * 3, First_X, U, choas_select);
            choas_creation_oct();
        }

        private void choas_creation(int length, double First_X, double U, ChoasEnum choas_select)
        {
            choas_number_bin = new double[length + 1];
            choas_number_bin[0] = First_X;
            for (int i = 1; i <= length; i++)
            {
                switch (choas_select)
                {
                    case ChoasEnum.First:
                        {
                            choas_number_bin[i] = get_choas_value_PWLCM(choas_number_bin[i - 1], U);
                            break;
                        }
                    case ChoasEnum.Secend:
                        {
                            choas_number_bin[i] = get_choas_value(choas_number_bin[i - 1], U);
                            break;
                        }
                }
            }
            for (int i = 1; i <= length; i++)
            {
                choas_number_bin[i] = Math.Round(choas_number_bin[i]);
            }
        }
        private double get_choas_value_PWLCM(double X, double U)
        {
            if (X < U)
            {
                return X / U;
            }
            else if (X <= 0.5)
            {
                return (X - U) / (0.5 - U);
            }
            else
            {
                return get_choas_value_PWLCM(1 - X, U);
            }
        }
        private double get_choas_value(double X, double U)
        {
            return U * X * (1 - X);
        }
        private void choas_creation_oct()
        {
            choas_number_oct = new double[choas_number_bin.Length / 3];
            for (int i = 0; i < choas_number_oct.Length; i++)
            {
                choas_number_oct[i] = bin2oct(choas_number_bin[i * 3 + 1], choas_number_bin[i * 3 + 2], choas_number_bin[i * 3 + 3]);
            }
        }
        private double bin2oct(double x1, double x2, double x3)
        {
            return x3 + x2 * 2 + x1 * 4;
        }

        public void encrypt(byte[] file_sequence)
        {
            byte A1, A2, B1, B2, X1, X2, Z1, Z2, C1, C2;
            for (int j = 0; j <= 5; j++)
            {
                for (int i = 0; i < file_sequence.Length; i = i + 2)
                {
                    A1 = file_sequence[i];
                    A2 = file_sequence[i + 1];
                    Z1 = Convert.ToByte(choas_number_oct[i]);
                    Z2 = Convert.ToByte(choas_number_oct[i + 1]);
                    B1 = crossover(A1, A2);
                    B2 = crossover(A2, A1);
                    X1 = make_x(Z1);
                    X2 = make_x(Z2);
                    C1 = XNOR(B1, X1);
                    C2 = XNOR(B2, X2);
                    file_sequence[i] = C1;
                    file_sequence[i + 1] = C2;
                }
            }
            encrypt_information = new byte[file_sequence.Length];
            for (int i = 0; i < file_sequence.Length; i++)
            {
                encrypt_information[i] = file_sequence[i];
            }
        }
        private byte crossover(byte A1, byte A2)
        {
            int temp = A1 & 248 | A2 & 7;
            return Convert.ToByte(temp);
        }
        private byte make_x(byte Z)
        {
            int temp = Z ^ Z << 4;
            return Convert.ToByte(temp);
        }
        private byte XNOR(byte B, byte X)
        {
            byte temp = Convert.ToByte(B ^ X);
            return temp;
        }
    }
}
