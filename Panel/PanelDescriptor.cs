﻿using Panel.Communication;

namespace Panel
{
    public class PanelDescriptor
    {
        public byte? DisplayCount { get; set; } = null;
        public DisplayTypes[]? DisplayTypes { get; set; } = null;
        public byte[][]? DisplayDescriptor { get; set; } = null;
        public byte? ButtonCount { get; set; } = null;
        public byte? AbsoluteCount { get; set; } = null;

        public class Serializable
        {

        }

        public PanelDescriptor()
        {
        }
    }
}