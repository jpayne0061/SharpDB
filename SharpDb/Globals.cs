﻿using System;
using System.Collections.Generic;
using System.Text;

namespace SharpDb
{
    public static class Globals
    {
        public static string FILE_NAME = "data.txt";
        public static byte INDEX_PAGE_HEADER_LENGTH = 32;
        public static short TABLE_DEF_LENGTH = 530;
        //24 bytes each column, 20 columns max = 480. + 41 bytes for name (string length 20)
        //plus 8 bytes for data location

        public static char EndTableDefinition = '|';

        public static short BooleanByteLength = 1;
        public static short CharByteLength = 2;
        public static short DecimalByteLength = 16;
        public static short Int32ByteLength = 4;
        public static short Int64ByteLength = 8;

        public static byte BooleanType = 0;
        public static byte CharType = 1;
        public static byte DecimalType = 2;
        public static byte Int32Type = 3;
        public static byte Int64Type = 4;
        public static byte StringType = 5;
    }
}
