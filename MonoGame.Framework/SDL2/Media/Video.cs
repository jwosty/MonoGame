#region License
/* FNA - XNA4 Reimplementation for Desktop Platforms
 * Copyright 2009-2014 Ethan Lee and the MonoGame Team
 *
 * Released under the Microsoft Public License.
 * See LICENSE for details.
 */
#endregion

#region VIDEOPLAYER_OPENGL Option
/* By default we use a small fragment shader to perform the YUV-RGBA conversion.
 * If for some reason you need to use the software converter in TheoraPlay,
 * comment out this define.
 * -flibit
 */
#define VIDEOPLAYER_OPENGL
#endregion

#region Using Statements
using System;
using System.IO;
using System.Threading;
#endregion

namespace Microsoft.Xna.Framework.Media
{
	public sealed class Video : IDisposable
	{
		#region Public Properties

		public int Width
		{
			get;
			private set;
		}

		public int Height
		{
			get;
			private set;
		}

		public string FileName
		{
			get;
			private set;
		}

		public float FramesPerSecond
		{
			get;
			internal set;
		}

		public VideoSoundtrackType VideoSoundtrackType
		{
			get;
			private set;
		}

		// FIXME: This is hacked, look up "This is a part of the Duration hack!"
		public TimeSpan Duration
		{
			get;
			internal set;
		}

		#endregion

		#region Internal Properties

		internal bool IsDisposed
		{
			get;
			private set;
		}

		internal bool AttachedToPlayer
		{
			get;
			set;
		}

		#endregion

		#region Internal Variables: TheoraPlay

		internal IntPtr theoraDecoder;
		internal IntPtr videoStream;

		#endregion

		#region Internal Constructors

		internal Video(string fileName)
		{
			FileName = fileName;

			// Set everything to NULL. Yes, this actually matters later.
			theoraDecoder = IntPtr.Zero;
			videoStream = IntPtr.Zero;

			// Initialize the decoder nice and early...
			IsDisposed = true;
			AttachedToPlayer = false;
			Initialize();

			// FIXME: This is a part of the Duration hack!
			Duration = TimeSpan.MaxValue;
		}

		internal Video(
			string fileName,
			int durationMS,
			int width,
			int height,
			float framesPerSecond,
			VideoSoundtrackType soundtrackType
		) : this(fileName) {
			/* If you got here, you've still got the XNB file! Well done!
			 * Except if you're running FNA, you're not using the WMV anymore.
			 * But surely it's the same video, right...?
			 * Well, consider this a check more than anything. If this bothers
			 * you, just remove the XNB file and we'll read the OGV straight up.
			 * -flibit
			 */
			if (width != Width || height != Height)
			{
				throw new Exception("XNB/OGV width/height mismatch!");
			}
			if (!MathHelper.WithinEpsilon(FramesPerSecond, framesPerSecond))
			{
				throw new Exception("XNB/OGV framesPerSecond mismatch!");
			}

			// FIXME: Oh, hey! I wish we had this info in TheoraPlay!
			Duration = TimeSpan.FromMilliseconds(durationMS);

			VideoSoundtrackType = soundtrackType;
		}

		#endregion

		#region Public Dispose Method

		public void Dispose()
		{
			if (AttachedToPlayer)
			{
				return; // NOPE. VideoPlayer will do the honors.
			}

			// Stop and unassign the decoder.
			if (theoraDecoder != IntPtr.Zero)
			{
				TheoraPlay.THEORAPLAY_stopDecode(theoraDecoder);
				theoraDecoder = IntPtr.Zero;
			}

			// Free and unassign the video stream.
			if (videoStream != IntPtr.Zero)
			{
				TheoraPlay.THEORAPLAY_freeVideo(videoStream);
				videoStream = IntPtr.Zero;
			}

			IsDisposed = true;
		}

		#endregion

		#region Internal TheoraPlay Initialization

		internal void Initialize()
		{
			if (!IsDisposed)
			{
				Dispose(); // We need to start from the beginning, don't we? :P
			}

			// Initialize the decoder.
			theoraDecoder = TheoraPlay.THEORAPLAY_startDecodeFile(
				FileName,
				150, // Max frames to buffer.  Arbitrarily set 5 seconds, assuming 30fps.
#if VIDEOPLAYER_OPENGL
				TheoraPlay.THEORAPLAY_VideoFormat.THEORAPLAY_VIDFMT_IYUV
#else
				// Use the TheoraPlay software converter.
				TheoraPlay.THEORAPLAY_VideoFormat.THEORAPLAY_VIDFMT_RGBA
#endif
			);

			// Wait until the decoder is ready.
			while (TheoraPlay.THEORAPLAY_isInitialized(theoraDecoder) == 0)
			{
				Thread.Sleep(10);
			}

			// Initialize the video stream pointer and get our first frame.
			if (TheoraPlay.THEORAPLAY_hasVideoStream(theoraDecoder) != 0)
			{
				while (videoStream == IntPtr.Zero)
				{
					videoStream = TheoraPlay.THEORAPLAY_getVideo(theoraDecoder);
					Thread.Sleep(10);
				}

				TheoraPlay.THEORAPLAY_VideoFrame frame = TheoraPlay.getVideoFrame(videoStream);

				// We get the FramesPerSecond from the first frame.
				FramesPerSecond = (float) frame.fps;
				Width = (int) frame.width;
				Height = (int) frame.height;
			}

			IsDisposed = false;
		}

		#endregion
	}
}
