#region License
//
// The Open Toolkit Library License
//
// Copyright (c) 2006 - 2009 the Open Toolkit library.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights to 
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
// the Software, and to permit persons to whom the Software is furnished to do
// so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
//
#endregion

using System;

namespace OpenTK.Platform
{
    using Graphics;

    sealed class Factory : IPlatformFactory
    {
        #region Fields

        static IPlatformFactory default_implementation;

        #endregion

        #region Constructors

        static Factory()
        {
            if (Configuration.RunningOnWindows) Default = new Windows.WinFactory();
            else if (Configuration.RunningOnMacOS) Default = new MacOS.MacOSFactory();
            else if (Configuration.RunningOnX11) Default = new X11.X11Factory();
            else Default = new UnsupportedPlatform();
        }

        #endregion

        #region Public Members

        public static IPlatformFactory Default
        {
            get { return default_implementation; }
            private set { default_implementation = value; }
        }

        #endregion

        #region IPlatformFactory Members

        public INativeWindow CreateNativeWindow(int x, int y, int width, int height, string title,
            GraphicsMode mode, GameWindowFlags options, DisplayDevice device)
        {
            return default_implementation.CreateNativeWindow(x, y, width, height, title, mode, options, device);
        }

        public IDisplayDeviceDriver CreateDisplayDeviceDriver()
        {
            return default_implementation.CreateDisplayDeviceDriver();
        }

        public IGraphicsContext CreateGLContext(GraphicsMode mode, IWindowInfo window, int major, int minor)
        {
            return default_implementation.CreateGLContext(mode, window, major, minor);
        }

        public GraphicsContext.GetCurrentContextDelegate CreateGetCurrentGraphicsContext()
        {
            return default_implementation.CreateGetCurrentGraphicsContext();
        }

        public IGraphicsMode CreateGraphicsMode()
        {
            return default_implementation.CreateGraphicsMode();
        }

        class UnsupportedPlatform : IPlatformFactory
        {
            #region Fields
            
            static readonly string error_string = "Please, refer to http://www.opentk.com for more information.";
            
            #endregion
            
            #region IPlatformFactory Members

            public INativeWindow CreateNativeWindow(int x, int y, int width, int height, string title, GraphicsMode mode, GameWindowFlags options, DisplayDevice device)
            {
                throw new PlatformNotSupportedException(error_string);
            }

            public IDisplayDeviceDriver CreateDisplayDeviceDriver()
            {
                throw new PlatformNotSupportedException(error_string);
            }

            public IGraphicsContext CreateGLContext(GraphicsMode mode, IWindowInfo window, int major, int minor)
            {
                throw new PlatformNotSupportedException(error_string);
            }

            public GraphicsContext.GetCurrentContextDelegate CreateGetCurrentGraphicsContext()
            {
                throw new PlatformNotSupportedException(error_string);
            }

            public IGraphicsMode CreateGraphicsMode()
            {
                throw new PlatformNotSupportedException(error_string);
            }
            
            #endregion
        }

        #endregion
    }
}