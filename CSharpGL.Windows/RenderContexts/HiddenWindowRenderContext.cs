﻿using System;
using System.Diagnostics;

namespace CSharpGL
{
    /// <summary>
    /// creates render device and render context.
    /// </summary>
    public class HiddenWindowRenderContext : GLRenderContext
    {
        /// <summary>
        /// Creates the render context provider. Must also create the OpenGL extensions.
        /// </summary>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="bitDepth">The bit depth.</param>
        /// <param name="parameter">The parameter</param>
        /// <returns></returns>
        public override bool Create(int width, int height, int bitDepth, object parameter)
        {
            //  Call the base.
            base.Create(width, height, bitDepth, parameter);

            // Create a new window class, as basic as possible.
            if (!this.CreateBasicRenderContext(width, height, bitDepth)) { return false; }

            //	Create the render context.
            this.RenderContextHandle = Win32.wglCreateContext(this.DeviceContextHandle);

            //  Make the context current.
            this.MakeCurrent();

            //  Update the context if required.
            // if I update context, something in legacy opengl will not work...
            //this.UpdateContextVersion();

            //  Return success.
            return true;
        }

        /// <summary>
        /// Create a new window class, as basic as possible.
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="bitDepth"></param>
        /// <returns></returns>
        private bool CreateBasicRenderContext(int width, int height, int bitDepth)
        {
            var wndClass = new WNDCLASSEX();
            wndClass.Init();
            wndClass.style = ClassStyles.HorizontalRedraw | ClassStyles.VerticalRedraw | ClassStyles.OwnDC;
            wndClass.lpfnWndProc = wndProcDelegate;
            wndClass.cbClsExtra = 0;
            wndClass.cbWndExtra = 0;
            wndClass.hInstance = IntPtr.Zero;
            wndClass.hIcon = IntPtr.Zero;
            wndClass.hCursor = IntPtr.Zero;
            wndClass.hbrBackground = IntPtr.Zero;
            wndClass.lpszMenuName = null;
            wndClass.lpszClassName = "CSharpGLRenderWindow";
            wndClass.hIconSm = IntPtr.Zero;
            Win32.RegisterClassEx(ref wndClass);

            //	Create the window. Position and size it.
            windowHandle = Win32.CreateWindowEx(0,
                          "CSharpGLRenderWindow",
                          "",
                          WindowStyles.WS_CLIPCHILDREN | WindowStyles.WS_CLIPSIBLINGS | WindowStyles.WS_POPUP,
                          0, 0, width, height,
                          IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);

            //	Get the window device context.
            this.DeviceContextHandle = Win32.GetDC(windowHandle);

            //	Setup a pixel format.
            var pfd = new PixelFormatDescriptor();
            pfd.Init();
            pfd.nVersion = 1;
            pfd.dwFlags = Win32.PFD_DRAW_TO_WINDOW | Win32.PFD_SUPPORT_OPENGL | Win32.PFD_DOUBLEBUFFER;
            pfd.iPixelType = Win32.PFD_TYPE_RGBA;
            pfd.cColorBits = (byte)bitDepth;
            pfd.cDepthBits = 16;
            pfd.cStencilBits = 8;
            pfd.iLayerType = Win32.PFD_MAIN_PLANE;

            //	Match an appropriate pixel format
            int iPixelformat = Win32.ChoosePixelFormat(this.DeviceContextHandle, pfd);
            if (iPixelformat == 0)
            {
                return false;
            }

            //	Sets the pixel format
            if (Win32.SetPixelFormat(this.DeviceContextHandle, iPixelformat, pfd) == 0)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Only valid to be called after the render context is created, this function attempts to
        /// move the render context to the OpenGL version originally requested. If this is &gt; 2.1, this
        /// means building a new context. If this fails, we'll have to make do with 2.1.
        /// </summary>
        protected void UpdateContextVersion()
        {
            //  If the request version number is anything up to and including 2.1, standard render contexts
            //  will provide what we need (as long as the graphics card drivers are up to date).

            //  Now the none-trivial case. We must use the WGL_create_context extension to
            //  attempt to create a 3.0+ context.
            int major, minor;
            GetHighestVersion(out major, out minor);
            if ((major > 2) || (major == 2 && minor > 1))
            {
                try
                {
                    //OpenGL.WGL_CONTEXT_MAJOR_VERSION_ARB, requestedVersionNumber.Major,  kw
                    //OpenGL.WGL_CONTEXT_MINOR_VERSION_ARB, requestedVersionNumber.Minor,
                    //OpenGL.WGL_CONTEXT_FLAGS_ARB, OpenGL.WGL_CONTEXT_FORWARD_COMPATIBLE_BIT_ARB,

                    int[] attributes =
                    {
                        GL.WGL_CONTEXT_MAJOR_VERSION, major,
                        GL.WGL_CONTEXT_MINOR_VERSION, minor,
                        GL.WGL_CONTEXT_FLAGS, GL.WGL_CONTEXT_FORWARD_COMPATIBLE_BIT,// compatible profile
//#if DEBUG
//                        GL.WGL_CONTEXT_FLAGS, GL.WGL_CONTEXT_DEBUG_BIT,// this is a debug context
//#endif
                        0
                    };
                    var wglCreateContextAttribs = GL.Instance.GetDelegateFor("wglCreateContextAttribsARB", GLDelegates.typeof_IntPtr_IntPtr_IntPtr_intN) as GLDelegates.IntPtr_IntPtr_IntPtr_intN;
                    IntPtr hrc = wglCreateContextAttribs(this.DeviceContextHandle, IntPtr.Zero, attributes);
                    Win32.wglMakeCurrent(IntPtr.Zero, IntPtr.Zero);
                    Win32.wglDeleteContext(this.RenderContextHandle);
                    Win32.wglMakeCurrent(this.DeviceContextHandle, hrc);
                    this.RenderContextHandle = hrc;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
            }
        }

        private void GetHighestVersion(out int major, out int minor)
        {
            major = 2; minor = 1;
            try
            {
                string version = GL.Instance.GetString(GL.GL_VERSION);
                string[] parts = version.Split('.');
                major = int.Parse(parts[0]);
                minor = int.Parse(parts[1]);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        private static WndProc wndProcDelegate = new WndProc(WndProc);

        static private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            return Win32.DefWindowProc(hWnd, msg, wParam, lParam);
        }

        /// <summary>
        /// Destroys the render context provider instance.
        /// </summary>
        protected override void DisposeUnmanagedResources()
        {
            //	Release the device context.
            Win32.ReleaseDC(windowHandle, this.DeviceContextHandle);

            //	Destroy the window.
            Win32.DestroyWindow(windowHandle);

            // If we have a render context, destroy it.
            if (this.RenderContextHandle != IntPtr.Zero)
            {
                Win32.wglDeleteContext(this.RenderContextHandle);
                this.RenderContextHandle = IntPtr.Zero;
            }
        }

        ///// <summary>
        ///// Sets the dimensions of the render context provider.
        ///// </summary>
        ///// <param name="width">Width.</param>
        ///// <param name="height">Height.</param>
        //public override void SetDimensions(int width, int height)
        //{
        //    //  Call the base.
        //    base.SetDimensions(width, height);

        //    //	Set the window size.
        //    Win32.SetWindowPos(windowHandle, IntPtr.Zero, 0, 0, Width, Height,
        //        SetWindowPosFlags.SWP_NOACTIVATE |
        //        SetWindowPosFlags.SWP_NOCOPYBITS |
        //        SetWindowPosFlags.SWP_NOMOVE |
        //        SetWindowPosFlags.SWP_NOOWNERZORDER);
        //}

        /// <summary>
        /// Blit the rendered data to the supplied device context.
        /// </summary>
        /// <param name="deviceContext">The HDC.</param>
        public override void Blit(IntPtr deviceContext)
        {
            IntPtr dc = this.DeviceContextHandle;
            if (dc != IntPtr.Zero || windowHandle != IntPtr.Zero)
            {
                //	Swap the buffers.
                Win32.SwapBuffers(dc);

                //	Blit the DC (containing the DIB section) to the target DC.
                Win32.BitBlt(deviceContext, 0, 0, this.Width, this.Height, dc, 0, 0, Win32.SRCCOPY);
            }
        }

        /// <summary>
        /// Makes the render context current.
        /// </summary>
        public override void MakeCurrent()
        {
            if (this.RenderContextHandle != IntPtr.Zero)
                Win32.wglMakeCurrent(this.DeviceContextHandle, this.RenderContextHandle);
        }

        /// <summary>
        /// The window handle.
        /// </summary>
        protected IntPtr windowHandle = IntPtr.Zero;
    }
}