using System;


namespace ChipAte.Console;

struct Instruction
{
    public ushort opcode;
    public ushort firstNibble;
    public ushort x;
    public ushort y;
    public ushort n;
    public ushort kk;
    public ushort nnn;
}

public class Chip8
{
    // documentation reference: https://en.wikipedia.org/wiki/CHIP-8#Opcode_table
    // and http://devernay.free.fr/hacks/chip8/C8TECH10.HTM
    // and then https://github.com/Timendus/chip8-test-suite?tab=readme-ov-file for tests
    // but note this errata - https://github.com/gulrak/cadmium/wiki/CTR-Errata 
    // and https://www.laurencescotford.net/2020/07/25/chip-8-on-the-cosmac-vip-index/ 
    // and https://tobiasvl.github.io/blog/write-a-chip-8-emulator/ 
    // and https://chip8.gulrak.net/ 


    // TODO: change Memory, Registers, Keypad so that we access them through methods which can protect
    // against OOB, rather than just throwing arrays around everywhere.
    private const int MEMORY_SIZE = 4096;
    private byte[] memory;
    private const int START_ADDRESS = 0x200;

    public const int DISPLAY_WIDTH = 64;
    public const int DISPLAY_HEIGHT = 32;

    private const int FONTSET_START_ADDRESS = 0x50;

    private Random rand = new Random();

    private byte[] fontset =
    {
        0xF0, 0x90, 0x90, 0x90, 0xF0, // 0
        0x20, 0x60, 0x20, 0x20, 0x70, // 1
        0xF0, 0x10, 0xF0, 0x80, 0xF0, // 2
        0xF0, 0x10, 0xF0, 0x10, 0xF0, // 3
        0x90, 0x90, 0xF0, 0x10, 0x10, // 4
        0xF0, 0x80, 0xF0, 0x10, 0xF0, // 5
        0xF0, 0x80, 0xF0, 0x90, 0xF0, // 6
        0xF0, 0x10, 0x20, 0x40, 0x40, // 7
        0xF0, 0x90, 0xF0, 0x90, 0xF0, // 8
        0xF0, 0x90, 0xF0, 0x10, 0xF0, // 9
        0xF0, 0x90, 0xF0, 0x90, 0x90, // A
        0xE0, 0x90, 0xE0, 0x90, 0xE0, // B
        0xF0, 0x80, 0x80, 0x80, 0xF0, // C
        0xE0, 0x90, 0x90, 0x90, 0xE0, // D
        0xF0, 0x80, 0xF0, 0x80, 0xF0, // E
        0xF0, 0x80, 0xF0, 0x80, 0x80  // F
    };

    private byte[] registers = new byte[16];
    private ushort[] stack = new ushort[16];
    public int DisplayWidth { get { return DISPLAY_WIDTH; } }
    public int DisplayHeight { get { return DISPLAY_HEIGHT; } }
    private byte[] screen = new byte[DISPLAY_WIDTH * DISPLAY_HEIGHT];
    public byte[] Screen { get { return screen; } }

    private bool romLoaded = false;
    public bool ROMLoaded { get { return romLoaded; } }
    
    private string romPath = string.Empty;
    public string ROMPath { get { return romPath; } }

    
    public bool DidDXYN {  get {  return instruction.firstNibble == 0xD; } }

    private ushort pc; // Program Counter
    private ushort sp; // Stack Pointer
    private ushort i;  // Index Register

    private bool[] keypad = new bool[16];
    private bool[] prevKeypad = new bool[16];
    public bool[] Keypad { get { return keypad; } }

    private byte timerDelay;
    public byte TimerDelay { get { return timerDelay; } set { timerDelay = value; } } 
    
    private byte timerSound;
    public byte TimerSound { get { return timerSound; } set { timerSound = value; } }

    private Instruction instruction;

    public Chip8()
    {
        SoftReset();
    }


    // Implementation of fetch operation
    public void Fetch()
    {
        instruction.opcode = (ushort)(memory[pc] << 8 | memory[pc + 1]);
        pc = (ushort)(pc + 2);
    }


    // Decode the opcode into our instruction struct
    public void Decode()
    {
        instruction.firstNibble = (ushort)((instruction.opcode & 0xF000) >> 12);
        instruction.x = (ushort)((instruction.opcode & 0x0F00) >> 8);
        instruction.y = (ushort)((instruction.opcode & 0x00F0) >> 4);
        instruction.n = (ushort)(instruction.opcode & 0x000F);
        instruction.kk = (ushort)(instruction.opcode & 0x00FF);
        instruction.nnn = (ushort)(instruction.opcode & 0x0FFF);
    }

    // Execute the current decoded instruction
    public void Execute()
    {
        switch (instruction.firstNibble)
        {
            case 0x0:
                if (instruction.opcode == 0x00E0)
                {
                    // Clear the display
                    Array.Clear(screen, 0, screen.Length);
                }
                else if (instruction.opcode == 0x00EE)
                {
                    // Return from subroutine
                    sp--;
                    pc = stack[sp];
                }

                // 0nnn - SYS addr
                // Jump to a machine code routine at nnn.
                // This instruction is only used on the old computers on which Chip - 8 was originally implemented.
                // It is ignored by modern interpreters.

                // fall through for all 0x0 opcodes
                break;

            case 0x1:
                // Jump to address NNN
                pc = instruction.nnn;
                break;

            case 0x2:
                // Call subroutine at NNN
                stack[sp] = pc;
                sp++;
                pc = instruction.nnn;
                break;

            case 0x3:
                // 3xkk - SE Vx, byte
                // Skip next instruction if Vx = kk.
                // The interpreter compares register Vx to kk, and if they are equal, increments the program counter by 2.
                if (registers[instruction.x] == (byte)instruction.kk)
                {
                    pc += 2;
                }
                break;

            case 0x4:
                // 4xkk - SNE Vx, byte
                // Skip next instruction if Vx != kk.
                // The interpreter compares register Vx to kk,
                // and if they are not equal, increments the program counter by 2.
                if (registers[instruction.x] != (byte)instruction.kk)
                {
                    pc += 2;
                }
                break;

            case 0x5:
                // 5xy0 - SE Vx, Vy
                // Skip next instruction if Vx = Vy.
                // The interpreter compares register Vx to register Vy,
                // and if they are equal, increments the program counter by 2.
                if (registers[instruction.x] == registers[instruction.y])
                {
                    pc += 2;
                }
                break;

            case 0x6:
                // 6xkk - LD Vx, byte
                // Set Vx = kk.
                // The interpreter puts the value kk into register Vx.
                registers[instruction.x] = (byte)instruction.kk;
                break;

            case 0x7:
                // 7xkk - ADD Vx, byte
                // Set Vx = Vx + kk.
                // Adds the value kk to the value of register Vx, then stores the result in Vx. 
                registers[instruction.x] += (byte)instruction.kk;
                break;

            case 0x8:
                // these are a bit messier, so let's deal with them elsewhere
                HandleOpcode8();
                break;

            case 0x9:
                // 9xy0 - SNE Vx, Vy
                // Skip next instruction if Vx != Vy.
                // The values of Vx and Vy are compared, and if they are not equal, the program counter is increased by 2.
                if (registers[instruction.x] != registers[instruction.y])
                {
                    pc += 2;
                }
                break;

            case 0xA:
                // Annn - LD I, addr
                // Set I = nnn.
                // The value of register I is set to nnn.
                i = instruction.nnn;
                break;

            case 0xB:
                // Bnnn - JP V0, addr
                // Jump to location nnn + V0.
                // The program counter is set to nnn plus the value of V0.
                pc = (ushort)(instruction.nnn + registers[0]);
                break;
            case 0xC:
                // Cxkk - RND Vx, byte
                // Set Vx = random byte AND kk.
                // The interpreter generates a random number from 0 to 255,
                // which is then ANDed with the value kk. The results are stored in Vx.
                byte randomByte = (byte)rand.Next(0, 256);
                registers[instruction.x] = (byte)(randomByte & instruction.kk);
                break;

            case 0xD:
                HandleOpcodeD();
                break;

            case 0xE:
                HandleOpcodeE();
                break;

            case 0xF:
                HandleOpcodeF();
                break;

            // Additional or 'superset' opcode implementations go here
            default:
                throw new NotImplementedException($"Opcode {instruction.opcode:X4} not implemented.");
        }
    }

    private void HandleOpcode8()
    {
        switch (instruction.n)
        {
            case 0x0:
                {
                    // 8xy0 - LD Vx, Vy
                    // Set Vx = Vy.
                    // Stores the value of register Vy in register Vx.
                    registers[instruction.x] = registers[instruction.y];
                    break;
                }
            case 0x1:
                {
                    // 8xy1 - OR Vx, Vy
                    // Set Vx = Vx OR Vy.
                    // Performs a bitwise OR on the values of Vx and Vy, then stores the result in Vx.
                    registers[instruction.x] |= registers[instruction.y];
                    registers[0xF] = 0; // According to some implementations, VF is set to 0 for this opcode
                    break;
                }
            case 0x2:
                {
                    // 8xy2 - AND Vx, Vy
                    // Set Vx = Vx AND Vy.
                    // Performs a bitwise AND on the values of Vx and Vy, then stores the result in Vx.
                    registers[instruction.x] &= registers[instruction.y];
                    registers[0xF] = 0; // According to some implementations, VF is set to 0 for this opcode
                    break;
                }
            case 0x3:
                {
                    // 8xy3 - XOR Vx, Vy
                    // Set Vx = Vx XOR Vy.
                    // Performs a bitwise exclusive OR on the values of Vx and Vy, then stores the result in Vx.
                    registers[instruction.x] ^= registers[instruction.y];
                    registers[0xF] = 0; // According to some implementations, VF is set to 0 for this opcode
                    break;
                }
            case 0x4:
                {
                    // 8xy4 - ADD Vx, Vy
                    // Set Vx = Vx + Vy, set VF = carry.
                    // The values of Vx and Vy are added together.
                    // If the result is greater than 8 bits (i.e., > 255), VF is set to 1, otherwise 0.
                    // Only the lowest 8 bits of the result are kept, and stored in Vx.
                    int sum = registers[instruction.x] + registers[instruction.y];
                    int carry = (byte)(sum > 255 ? 1 : 0);
                    registers[instruction.x] = (byte)(sum & 0xFF);
                    registers[0xF] = (byte)carry;
                    break;
                }
            case 0x5:
                {
                    // 8xy5 - SUB Vx, Vy
                    // Set Vx = Vx - Vy, set VF = NOT borrow.
                    // If Vx > Vy, then VF is set to 1, otherwise 0. Then Vy is subtracted from Vx, and the results stored in Vx.               
                    int sum = (byte)(registers[instruction.x] - registers[instruction.y]);
                    int flag = (byte)(registers[instruction.x] >= registers[instruction.y] ? 1 : 0);
                    registers[instruction.x] = (byte)sum;
                    registers[0xF] = (byte)flag;
                    break;
                }
            case 0x6:
                {
                    // 8xy6 - SHR Vx {, Vy}
                    // Set Vx = Vx SHR 1.
                    // If the least-significant bit of Vx is 1, then VF is set to 1, otherwise 0. Then Vx is divided by 2.

                    registers[instruction.x] = registers[instruction.y];    // CHIP-8 only behaviour!

                    int flag = (byte)(registers[instruction.x] & 0x1);
                    int sum = registers[instruction.x] >> 1;
                    registers[instruction.x] = (byte)sum;
                    registers[0xF] = (byte)flag;
                    break;
                }
            case 0x7:
                {
                    // 8xy7 - SUBN Vx, Vy
                    // Set Vx = Vy - Vx, set VF = NOT borrow.
                    // If Vy > Vx, then VF is set to 1, otherwise 0. Then Vx is subtracted from Vy, and the results stored in Vx.
                    registers[instruction.x] = (byte)(registers[instruction.y] - registers[instruction.x]);
                    registers[0xF] = (byte)(registers[instruction.y] >= registers[instruction.x] ? 1 : 0);
                    break;
                }
            case 0xE:
                {
                    // 8xyE - SHL Vx {, Vy}
                    // Set Vx = Vx SHL 1.
                    // If the most - significant bit of Vx is 1, then VF is set to 1, otherwise to 0. Then Vx is multiplied by 2.

                    registers[instruction.x] = registers[instruction.y];    // CHIP-8 only behaviour!

                    int flag = (byte)((registers[instruction.x] & 0x80) >> 7);
                    int sum = registers[instruction.x] << 1;
                    registers[instruction.x] = (byte)sum;
                    registers[0xF] = (byte)flag;
                    break;
                }
            // Additional cases for other 8xyN opcodes would go here
            default:
                throw new NotImplementedException($"Opcode 8xy{instruction.n:X} not implemented.");
        }
    }

    private void HandleOpcodeD()
    {
        byte vx = registers[instruction.x];
        byte vy = registers[instruction.y];
        int height = instruction.n;

        registers[0xF] = 0;

        // Wrap the START coordinate only
        int startX = vx % DISPLAY_WIDTH;     // 64
        int startY = vy % DISPLAY_HEIGHT;    // 32

        for (int row = 0; row < height; row++)
        {
            int yCoord = startY + row;
            if (yCoord >= DISPLAY_HEIGHT)
                break; // clip bottom: no more rows visible

            byte spriteByte = memory[i + row];

            for (int col = 0; col < 8; col++)
            {
                int xCoord = startX + col;
                if (xCoord >= DISPLAY_WIDTH)
                    break; // clip right: remaining cols also off-screen

                if ((spriteByte & (0x80 >> col)) == 0)
                    continue;

                int pixelIndex = xCoord + (yCoord * DISPLAY_WIDTH);

                if (screen[pixelIndex] == 1)
                    registers[0xF] = 1;

                screen[pixelIndex] ^= 1; // XOR: toggles 0<->1
            }
        }
    }

    private void HandleOpcodeE()
    {
        if (instruction.kk == 0x9E)
        {
            // Ex9E - SKP Vx
            // Skip next instruction if key with the value of Vx is pressed.
            // Checks the keyboard, and if the key corresponding to the value of Vx is
            // currently in the down position, PC is increased by 2.
            byte keyIndex = (byte)(registers[instruction.x] & 0xF);
            if (keypad[keyIndex] == true)
            {
                pc += 2;
            }
        }
        else if (instruction.kk == 0xA1)
        {
            // ExA1 - SKNP Vx
            // Skip next instruction if key with the value of Vx is not pressed.
            // Checks the keyboard, and if the key corresponding to the value of Vx is
            // currently in the up position, PC is increased by 2.
            byte keyIndex = (byte)(registers[instruction.x] & 0xF);

            if (keypad[keyIndex] == false)
            {
                pc += 2;
            }
        }
        else
        {
            throw new NotImplementedException($"Opcode Ex{instruction.kk:X2} not implemented.");
        }
    }

    private void HandleOpcodeF()
    {
        // Fx07 - LD Vx, DT
        // Set Vx = delay timer value.
        // The value of DT is placed into Vx.
        if (instruction.kk == 0x07)
        {
            registers[instruction.x] = (byte)timerDelay;
        }
        else if (instruction.kk == 0x0A)
        {
            // Fx0A - LD Vx, K
            // Wait for a key to be pressed and the released, store the value of the key in Vx.
            // All execution stops until a key is pressed and released, then the value of that key is stored in Vx.
            bool keyHandled = false;
            for (int key = 0; key < keypad.Length; key++)
            {
                if (prevKeypad[key] == true && keypad[key] == false)
                {
                    registers[instruction.x] = (byte)key;
                    keyHandled = true;
                    break;
                }
            }
            if (!keyHandled)
            {
                pc -= 2; // effectively repeat this instruction
            }
        }
        else if (instruction.kk == 0x15)
        {
            // Fx15 - LD DT, Vx
            // Set delay timer = Vx.
            // DT is set equal to the value of Vx.
            timerDelay = registers[instruction.x];
        }
        else if (instruction.kk == 0x18)
        {
            // Fx18 - LD ST, Vx
            // Set sound timer = Vx.
            // ST is set equal to the value of Vx.
            timerSound = registers[instruction.x];
        }
        else if (instruction.kk == 0x1E)
        {
            // Fx1E - ADD I, Vx
            // Set I = I + Vx.
            // The values of I and Vx are added, and the results are stored in I.
            i += registers[instruction.x];
        }
        else if (instruction.kk == 0x29)
        {
            // Fx29 - LD F, Vx
            // Set I = location of sprite for digit Vx.
            // The value of I is set to the location for the hexadecimal sprite
            // corresponding to the value of Vx.
            i = (ushort)(FONTSET_START_ADDRESS + (registers[instruction.x] * 5));
        }
        else if (instruction.kk == 0x33)
        {
            // Fx33 - LD B, Vx
            // Store BCD representation of Vx in memory locations I, I+1, and I+2.
            // The interpreter takes the decimal value of Vx, and places the hundreds
            // digit in memory at location in I, the tens digit at location I+1,
            // and the ones digit at location I+2.
            byte value = registers[instruction.x];
            memory[i] = (byte)(value / 100);
            memory[i + 1] = (byte)((value / 10) % 10);
            memory[i + 2] = (byte)(value % 10);
        }
        else if (instruction.kk == 0x55)
        {
            // Fx55 - LD [I], Vx
            // Store registers V0 through Vx in memory starting at location I.
            // The interpreter copies the values of registers V0 through Vx into memory,
            // starting at the address in I.
            int iOffset = 0;
            for (int regIndex = 0; regIndex <= instruction.x; regIndex++)
            {
                memory[i + regIndex] = registers[regIndex];
                iOffset++;
            }
            i += (ushort)iOffset;
        }
        else if (instruction.kk == 0x65)
        {
            // Fx65 - LD Vx, [I]
            // Read registers V0 through Vx from memory starting at location I.
            // The interpreter reads values from memory starting at location I into
            // registers V0 through Vx.
            int iOffset = 0;
            for (int regIndex = 0; regIndex <= instruction.x; regIndex++)
            {
                registers[regIndex] = memory[i + regIndex];
                iOffset++;
            }
            i += (ushort)iOffset;
        }
        else
        {
            throw new NotImplementedException($"Opcode Fx{instruction.kk:X2} not implemented.");
        }
    }


  
    public bool LoadRom(string FilePath)
    {
        try
        {
            byte[] romData = System.IO.File.ReadAllBytes(FilePath);
            if (romData.Length + START_ADDRESS > MEMORY_SIZE)
            {
                return false; // ROM too large to fit in memory
            }

            SoftReset();
            Array.Copy(romData, 0, memory, START_ADDRESS, romData.Length);
            romLoaded = true;
            romPath = FilePath;
            return true;
        }
        catch (Exception)
        {
            romLoaded = false;
            romPath = string.Empty;
            return false; // Failed to read file
        }
    }

    // it seems we need to be able to detect a change in keystate from one frame to another, so
    // we just have an internal copy of the previous keypad state
    public void SaveKeypad()
    {
       Array.Copy(keypad, prevKeypad, keypad.Length);
    }


    public void SoftReset()
    {
        memory = new byte[MEMORY_SIZE];
        Array.Copy(fontset, 0, memory, FONTSET_START_ADDRESS, fontset.Length);

        timerDelay = 0;
        timerSound = 0;
        registers = new byte[16];
        stack = new ushort[16];
        screen = new byte[DisplayWidth * DisplayHeight];
        keypad = new bool[16];
        prevKeypad = new bool[16];

        i = 0;
        pc = START_ADDRESS;
        sp = 0;
    }

}
