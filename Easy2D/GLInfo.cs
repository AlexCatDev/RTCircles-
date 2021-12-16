namespace Easy2D
{
    //Redo this
    public static class GLInfo
    {
        //This doesnt belong here but whatever
        public struct FrameInfo
        {
            public double InputTime;
            public double UpdateTime;
            public double RenderTime;
            public double SwapTime;

            public double Delta;

            public double Other => Delta - UpdateTime - RenderTime - SwapTime - InputTime;
        }

        public static FrameInfo LastFrameInfo = new FrameInfo();
    }
}
