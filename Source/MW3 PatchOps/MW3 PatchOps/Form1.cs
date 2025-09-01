using System;
using System.Windows.Forms;
using XRPCLib;

namespace MW3_PatchOps
{
    public partial class Form1 : Form
    {
        XRPC Jtag = new XRPC();
        private uint waveOffset = 0;
        private uint moneyOffset = 0;

        public Form1()
        {
            InitializeComponent();
        }

        private void ConnectButton_Click(object sender, EventArgs e)
        {
            try
            {
                Jtag.Connect();
                MessageBox.Show("Connected");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Connection failed: {ex.Message}");
            }
        }

        private void ToggleFeature(uint address, byte onValue, byte offValue, string featureName)
        {
            byte[] current = Jtag.GetMemory(address, 1);

            if (current == null || current.Length == 0)
            {
                MessageBox.Show($"Failed to read {featureName} memory");
                return;
            }

            if (current[0] == onValue)
            {
                Jtag.SetMemory(address, new byte[] { offValue });
                MessageBox.Show($"{featureName} Disabled");
            }
            else
            {
                Jtag.SetMemory(address, new byte[] { onValue });
                MessageBox.Show($"{featureName} Enabled");
            }
        }

        private void FindWaveButton_Click(object sender, EventArgs e)
        {
            const byte wildcard = 0x01;
            const int chunkSize = 0x1000;
            const uint startAddr = 0x83A97A80;
            const uint endAddr = 0x83B4D610;

            byte[] buffer = new byte[chunkSize];

            for (uint addr = startAddr; addr < endAddr; addr += (uint)chunkSize)
            {
                buffer = Jtag.GetMemory(addr, chunkSize);

                for (int i = 0; i <= buffer.Length - 8; i++)
                {
                    if (buffer[i] == 0x00 &&
                        buffer[i + 1] == 0x00 &&
                        buffer[i + 2] == 0x00 &&
                        buffer[i + 3] == wildcard &&
                        buffer[i + 6] == 0x06 &&
                        buffer[i + 7] == 0xBE)
                    {
                        waveOffset = addr + (uint)i + 1;
                        MessageBox.Show($"Wave offset found: 0x{waveOffset:X}");
                        return;
                    }
                }
            }
            MessageBox.Show("Pattern not found, wave must be '1' to find it");
        }

        private void PatchWaveButton_Click(object sender, EventArgs e)
        {
            if (waveOffset == 0)
            {
                MessageBox.Show("Wave offset not found yet, find it first!");
                return;
            }

            if (!int.TryParse(PatchWaveInput.Text, out int waveValue))
            {
                MessageBox.Show("Invalid input, please enter a valid integer");
                return;
            }

            byte[] full = new byte[4];
            full[0] = (byte)((waveValue >> 24) & 0xFF);
            full[1] = (byte)((waveValue >> 16) & 0xFF);
            full[2] = (byte)((waveValue >> 8) & 0xFF);
            full[3] = (byte)(waveValue & 0xFF);
            byte[] waveBytes = { full[1], full[2], full[3] };
            Jtag.SetMemory(waveOffset, waveBytes);
            MessageBox.Show($"Patched with value: 0x{waveValue:X}");
        }

        private void FindMoneyButton_Click(object sender, EventArgs e)
        {
            const int chunkSize = 0x1000;
            const uint startAddr = 0x83A00000;
            const uint endAddr = 0x83BFFFF0;
            byte[] buffer = new byte[chunkSize];

            for (uint addr = startAddr; addr < endAddr; addr += (uint)chunkSize)
            {
                buffer = Jtag.GetMemory(addr, chunkSize);

                for (int i = 0; i <= buffer.Length - 8; i++)
                {
                    if (buffer[i + 0] == 0x00 &&
                        buffer[i + 1] == 0x00 &&
                        buffer[i + 2] == 0x00 &&
                        buffer[i + 3] == 0x00 &&
                        buffer[i + 6] == 0x06 &&
                        buffer[i + 7] == 0xDB)
                    {
                        uint matchOffset = addr + (uint)i + 3;
                        moneyOffset = matchOffset - 3;
                        MessageBox.Show($"Money offset found: 0x{moneyOffset:X}");
                        return;
                    }
                }
            }
            MessageBox.Show("Money pattern not found, money must be '0' to be found"); //as we look for 0 it sometimes fucks up to find it, seems to work better if we look for 100 as starting money, tho that sucks more if youd want to put it into a menu
        }

        private void PatchMoneyButton_Click(object sender, EventArgs e)
        {
            if (moneyOffset == 0)
            {
                MessageBox.Show("Money offset not found yet, find it first!");
                return;
            }

            if (!int.TryParse(PatchMoneyInput.Text, out int moneyValue))
            {
                MessageBox.Show("Invalid input, please enter a valid integer");
                return;
            }

            if (moneyValue < 0 || moneyValue > 0xFFFFFF)
            {
                MessageBox.Show("Value too high, bitch");
                return;
            }

            byte[] moneyBytes =
            {
                (byte)((moneyValue >> 16) & 0xFF),
                (byte)((moneyValue >> 8) & 0xFF),
                (byte)(moneyValue & 0xFF)
            };

            Jtag.SetMemory(moneyOffset, moneyBytes);

            MessageBox.Show($"Money patched with value: {moneyValue} (0x{moneyValue:X})");//i know, the money isnt accurate patched > if you select 2000 you only get around 1700, but I dont give a shit so fuck you in advance
        }

        private void GodModeButton(object sender, EventArgs e) =>
            ToggleFeature(0x831500A3, 0x01, 0x00, "God Mode");

        private void NoclipButton(object sender, EventArgs e) =>
            ToggleFeature(0x8315AE97, 0x01, 0x00, "Noclip");

        private void SuperSpeedButton_Click(object sender, EventArgs e) =>
            ToggleFeature(0x83C5BF23, 0xFF, 0xBE, "Super Speed");

        private void SuperJumpButton_Click(object sender, EventArgs e) =>
            ToggleFeature(0x83C5A738, 0x44, 0x42, "Super Jump");

        private void ProFOVButton_Click(object sender, EventArgs e) =>
            ToggleFeature(0x83C53A0D, 0xB0, 0x82, "Pro FOV");

        private void WeaponBobButton(object sender, EventArgs e) =>
    ToggleFeature(0x83C595F4, 0x4F, 0x3D, "Weapon Bob");

        private void TimescaleButton(object sender, EventArgs e) =>
    ToggleFeature(0x83C474B1, 0xFF, 0x80, "Timescale");

        private void MoonGravityButton_Click(object sender, EventArgs e) =>
    ToggleFeature(0x83C5A2F8, 0x42, 0x44, "Moon Gravity");


        private void UnlimitedAmmoButton_Click(object sender, EventArgs e)
        {
            try
            {
                Jtag.SetMemory(0x83150462, new byte[] { 0x0F, 0xFF });
                Jtag.SetMemory(0x83150486, new byte[] { 0x0F, 0xFF });
                Jtag.SetMemory(0x8315046D, new byte[] { 0xFF });
                Jtag.SetMemory(0x83150479, new byte[] { 0xFF });
                Jtag.SetMemory(0x83150493, new byte[] { 0x45 });
                Jtag.SetMemory(0x8315049F, new byte[] { 0x45 });

                MessageBox.Show("Unlimited Ammo Patched!");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to patch Unlimited Ammo: {ex.Message}");
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Made by Dev___1_");
            MessageBox.Show("If you paste some of this into your tool then atleast give some credit and tell people to check out my free RME menu called 'God Menu Remake' for BO2");
        }
    }
}
