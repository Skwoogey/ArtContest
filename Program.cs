using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Timers;
using System.Threading;
//using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.Win32.SafeHandles;

/*
namespace ConsoleExtender
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ConsoleFont
    {
        public uint Index;
        public short SizeX, SizeY;
    }

    public static class ConsoleHelper
    {
        [DllImport("kernel32")]
        public static extern bool SetConsoleIcon(IntPtr hIcon);

        public static bool SetConsoleIcon(Icon icon)
        {
            return SetConsoleIcon(icon.Handle);
        }

        [DllImport("kernel32")]
        private extern static bool SetConsoleFont(IntPtr hOutput, uint index);

        private enum StdHandle
        {
            OutputHandle = -11
        }

        [DllImport("kernel32")]
        private static extern IntPtr GetStdHandle(StdHandle index);

        public static bool SetConsoleFont(uint index)
        {
            return SetConsoleFont(GetStdHandle(StdHandle.OutputHandle), index);
        }

        [DllImport("kernel32")]
        private static extern bool GetConsoleFontInfo(IntPtr hOutput, [MarshalAs(UnmanagedType.Bool)] bool bMaximize,
            uint count, [MarshalAs(UnmanagedType.LPArray), Out] ConsoleFont[] fonts);

        [DllImport("kernel32")]
        private static extern uint GetNumberOfConsoleFonts();

        public static uint ConsoleFontsCount
        {
            get
            {
                return GetNumberOfConsoleFonts();
            }
        }

        public static ConsoleFont[] ConsoleFonts
        {
            get
            {
                ConsoleFont[] fonts = new ConsoleFont[GetNumberOfConsoleFonts()];
                if (fonts.Length > 0)
                    GetConsoleFontInfo(GetStdHandle(StdHandle.OutputHandle), false, (uint)fonts.Length, fonts);
                return fonts;
            }
        }

    }
}
*/
public static class ConsoleHelper
{

    public enum FamilyType
    {
        TrueType = 54,
        PointType = 48
    }

    private const int StandardOutputHandle = -11;

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr GetStdHandle(int nStdHandle);

    [return: MarshalAs(UnmanagedType.Bool)]
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    internal static extern bool SetCurrentConsoleFontEx(IntPtr hConsoleOutput, bool MaximumWindow, ref FontInfo ConsoleCurrentFontEx);

    [return: MarshalAs(UnmanagedType.Bool)]
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    internal static extern bool GetCurrentConsoleFontEx(IntPtr hConsoleOutput, bool MaximumWindow, ref FontInfo ConsoleCurrentFontEx);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern IntPtr CreateConsoleScreenBuffer(uint desiredAccess, int SharedMode, IntPtr secAttribs, int flags, IntPtr ScreenBufferData);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern int SetConsoleActiveScreenBuffer(IntPtr handle);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern int WriteConsoleOutput(SafeFileHandle Handle, [MarshalAs(UnmanagedType.LPArray), In] CharInfo[] buf, Coord bufferSize, Coord bufferCoord, ref SmallRect writeRegion);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    internal static extern SafeFileHandle CreateFile(string fileName, [MarshalAs(UnmanagedType.U4)] uint fileAccess, [MarshalAs(UnmanagedType.U4)] uint fileShare, IntPtr securityAttributes, [MarshalAs(UnmanagedType.U4)] FileMode creationDisposition, [MarshalAs(UnmanagedType.U4)] int flags, IntPtr template);

    private static readonly IntPtr ConsoleOutputHandle = GetStdHandle(StandardOutputHandle);

    private static readonly SafeFileHandle h = CreateFile("CONOUT$", 0x40000000, 2, IntPtr.Zero, FileMode.Open, 0, IntPtr.Zero);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct FontInfo
    {
        internal int cbSize;
        internal int FontIndex;
        internal short FontWidth;
        public short FontSize;
        public int FontFamily;
        public int FontWeight;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        //[MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.wc, SizeConst = 32)]
        public string FontName;
    }

    public static void printFont(FontInfo font)
    {
        Console.WriteLine("cbSize: " + font.cbSize.ToString());
        Console.WriteLine("FontIndex: " + font.FontIndex.ToString());
        Console.WriteLine("FontWidth: " + font.FontWidth.ToString());
        Console.WriteLine("FontSize: " + font.FontSize.ToString());
        Console.WriteLine("FontFamily: " + font.FontFamily.ToString());
        Console.WriteLine("FontWeight: " + font.FontWeight.ToString());
        Console.WriteLine("FontName: " + font.FontName.ToString());
        Console.WriteLine();
    }

    public static FontInfo[] SetCurrentFont(string font, short fontSize = 0, short fontWidth = 0, FamilyType fontType = FamilyType.TrueType)
    {

        FontInfo before = new FontInfo
        {
            cbSize = Marshal.SizeOf<FontInfo>()
        };

        if (GetCurrentConsoleFontEx(ConsoleOutputHandle, false, ref before))
        {

            FontInfo set = new FontInfo
            {
                cbSize = Marshal.SizeOf<FontInfo>(),
                FontIndex = 0,
                FontFamily = (int)fontType,
                FontName = font,
                FontWeight = 400,
                FontSize = fontSize > 0 ? fontSize : before.FontSize,
                FontWidth = fontWidth > 0 ? fontWidth : before.FontWidth
            };

            // Get some settings from current font.
            if (!SetCurrentConsoleFontEx(ConsoleOutputHandle, false, ref set))
            {
                var ex = Marshal.GetLastWin32Error();
                Console.WriteLine("Set error " + ex);
                throw new System.ComponentModel.Win32Exception(ex);
            }

            FontInfo after = new FontInfo
            {
                cbSize = Marshal.SizeOf<FontInfo>()
            };
            GetCurrentConsoleFontEx(ConsoleOutputHandle, false, ref after);

            return new[] { before, set, after };
        }
        else
        {
            var er = Marshal.GetLastWin32Error();
            Console.WriteLine("Get error " + er);
            throw new System.ComponentModel.Win32Exception(er);
        }
    }



    public static IntPtr CreateNewScreenBuffer()
    {
        IntPtr BufferHandle = CreateConsoleScreenBuffer(0xE0000000, 0, IntPtr.Zero, 0, IntPtr.Zero);

        return BufferHandle;
    }

    public static int SetScreenBuffer(IntPtr Handle)
    {
        return SetConsoleActiveScreenBuffer(Handle);
    }

    [StructLayout(LayoutKind.Explicit, CharSet = CharSet.Unicode)]
    public struct CharInfo
    {
        [FieldOffset(0)] public byte sym;
        [FieldOffset(2)] public short Attributes;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct Coord
    {
        [FieldOffset(0)] public short X;
        [FieldOffset(2)] public short Y;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct SmallRect
    {
        [FieldOffset(0)] public short left;
        [FieldOffset(2)] public short top;
        [FieldOffset(4)] public short right;
        [FieldOffset(6)] public short bottom;
    }

    private static Coord bufSize;
    private static Coord bufCoord;
    private static SmallRect region;
    

    public static void initScreenVals(int width, int height)
    {
        bufSize = new Coord();
        bufCoord = new Coord();
        region = new SmallRect();

        short sw = (short)width;
        short sh = (short)height;

        bufSize.X = sw;
        bufSize.Y = sw;

        bufCoord.X = 0;
        bufCoord.Y = 0;

        region.left = 0;
        region.top = 0;
        region.right = (short)(width);
        region.bottom = (short)(height);
    }

    public static void writeAllBuffer(CharInfo[] buffer)
    {
        WriteConsoleOutput(h, buffer, bufSize, bufCoord, ref region);
    }
}

namespace ArtContest
{
    class Program
    {

        

        private static List<ConsoleHelper.CharInfo[]> clip_buffer = new List<ConsoleHelper.CharInfo[]>();
        private static int frame = 0;
        //static Stopwatch measurer = new Stopwatch();
        //static List<TimeSpan> tsList = new List<TimeSpan>();
        static System.Timers.Timer clipTimer = new System.Timers.Timer();
        static Stream s;
        static void Main(string[] args)
        {
            Stream audioStream = ArtContest.Properties.Resources.audio;

            System.Media.SoundPlayer player = new System.Media.SoundPlayer(audioStream);
            

            

            //ConsoleHelper.printFont(CHout[2]);

            int winWidth = 160;
            int winHeight = 90;

            ConsoleHelper.SetCurrentFont("Terminal", 8, 8, ConsoleHelper.FamilyType.PointType);

            if (Console.LargestWindowHeight < winHeight || Console.LargestWindowWidth < winWidth)
                ConsoleHelper.SetCurrentFont("Consolas", 6, 6, ConsoleHelper.FamilyType.PointType);

            if (Console.LargestWindowHeight < winHeight || Console.LargestWindowWidth < winWidth)
                ConsoleHelper.SetCurrentFont("Terminal", 4, 4, ConsoleHelper.FamilyType.PointType);

            if (Console.LargestWindowHeight < winHeight || Console.LargestWindowWidth < winWidth)
            {
                Console.WriteLine("Can't get small enough font, try decreasing system GUI scale and restarting.");
                Console.ReadKey();
                return;
            }

            Console.SetWindowSize(winWidth, winHeight);
            Console.SetBufferSize(winWidth, winHeight);
            Console.SetWindowPosition(0, 0);
            ConsoleHelper.initScreenVals(winWidth, winHeight);

            byte[] AllText = ArtContest.Properties.Resources.video;
            int frame_size_bytes = (winWidth) * winHeight * 4;
            int frame_size = (winWidth) * winHeight;
            int cur_index = 0;
            while(AllText.Length - cur_index >= frame_size_bytes)
            {
                
                ConsoleHelper.CharInfo[] CIframe = new ConsoleHelper.CharInfo[frame_size];
                unsafe
                {
                    fixed (byte* ptr = AllText.AsMemory(cur_index, frame_size_bytes).ToArray())
                    {
                        for(int i = 0; i < frame_size; i++)
                        {
                            CIframe[i] = ((ConsoleHelper.CharInfo*)ptr)[i];
                        }
                    }
                }

                clip_buffer.Add(CIframe);

                
                cur_index += frame_size_bytes;
                //Console.WriteLine(cur_index);
            }

            //Console.WriteLine(AllText.Length);

            clipTimer.Interval = 1000.0/60.0;
            clipTimer.Elapsed += OnTimedEvent;
            clipTimer.AutoReset = true;

            Console.WriteLine("\n\nDO NOT RESIZE WINDOW.\n\nThis is a cinematic experience. Move window so you can see it fully and hit enter.");

            Console.ReadKey();
            player.Play();
            clipTimer.Enabled = true;

            while (clipTimer.Enabled);

            player.Stop();

            Console.Clear();
            Console.WriteLine("\n\nI am sorry...");
            Console.ReadKey();

            /*
            foreach (var ts in tsList)
            {
                Console.WriteLine(ts.TotalMilliseconds);
            }
            Console.ReadKey();
            */
        }

        
        private static void OnTimedEvent(Object source, System.Timers.ElapsedEventArgs e)
        {
            //measurer.Restart();
            if (frame >= clip_buffer.Count)
                clipTimer.Enabled = false;

            ConsoleHelper.writeAllBuffer(clip_buffer[frame]);

            frame++;
            //measurer.Stop();
            //tsList.Add(measurer.Elapsed);
        }
        
    }
}
