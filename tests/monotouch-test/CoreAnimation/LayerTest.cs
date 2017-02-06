//
// Unit tests for CALayer
//
// Authors:
//	Sebastien Pouliot  <sebastien@xamarin.com>
//
// Copyright 2011 Xamarin Inc. All rights reserved.
//

#if !__WATCHOS__

using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
#if XAMCORE_2_0
using Foundation;
using CoreAnimation;
#else
using MonoTouch.CoreAnimation;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
#endif
using NUnit.Framework;

#if XAMCORE_2_0
using RectangleF=CoreGraphics.CGRect;
using SizeF=CoreGraphics.CGSize;
using PointF=CoreGraphics.CGPoint;
#else
using nfloat=global::System.Single;
using nint=global::System.Int32;
using nuint=global::System.UInt32;
#endif

namespace MonoTouchFixtures.CoreAnimation {
	
	[TestFixture]
	[Preserve (AllMembers = true)]
	public class LayerTest {
		
		[Test]
		public void Mask ()
		{
			using (CALayer layer = new CALayer ()) {
				Assert.Null (layer.Mask, "Mask/default");
				layer.Mask = new CALayer ();
				Assert.NotNull (layer.Mask, "Mask/assigned");
				layer.Mask = null;
				Assert.Null (layer.Mask, "Mask/nullable");
			}
		}
		
		[Test]
		public void CAActionTest ()
		{
			// bug 2441
			CAActionTestClass obj = new CAActionTestClass ();
			Assert.That (null == obj.ActionForKey ("animation"), "a");
			Assert.That (null == obj.Actions, "b");
			Assert.That (null == CAActionTestClass.DefaultActionForKey ("animation"), "c");
			
			var animationKey = new NSString ("animation");
			var basicAnimationKey = new NSString ("basicAnimation");
			var dict = NSDictionary.FromObjectsAndKeys (
				new NSObject [] { new CABasicAnimation (), new CAAnimation () },
				new NSObject [] { basicAnimationKey, animationKey } 
			);
			obj.Actions = dict;
			Assert.That (obj.Actions == dict, "d");
			
			Assert.That (obj.ActionForKey ("animation") == dict [animationKey], "e");
			Assert.That (obj.ActionForKey ("basicAnimation") == dict [basicAnimationKey], "f");
			Assert.That (null == CAActionTestClass.DefaultActionForKey ("animation"), "g");
			Assert.That (null == CALayer.DefaultActionForKey ("animation"), "h");
		}
		
		class CAActionTestClass : CALayer {
			
		}
		
		[Test]
		public void ConvertPoint ()
		{
			using (CALayer layer = new CALayer ()) {
				Assert.True (layer.ConvertPointFromLayer (PointF.Empty, null).IsEmpty, "From/Empty/null");
				Assert.True (layer.ConvertPointToLayer (PointF.Empty, null).IsEmpty, "To/Empty/null");
			}
		}

		[Test]
		public void ConvertRect ()
		{
			using (CALayer layer = new CALayer ()) {
				Assert.True (layer.ConvertRectFromLayer (RectangleF.Empty, null).IsEmpty, "From/Empty/null");
				Assert.True (layer.ConvertRectToLayer (RectangleF.Empty, null).IsEmpty, "To/Empty/null");
			}
		}
		
		[Test]
		public void ConvertTime ()
		{
			using (CALayer layer = new CALayer ()) {
				Assert.That (layer.ConvertTimeFromLayer (0.0d, null), Is.EqualTo (0.0d), "From/0.0d/null");
				Assert.That (layer.ConvertTimeToLayer (0.0d, null), Is.EqualTo (0.0d), "To/0.0d/null");
			}
		}

		[Test]
		public void AddAnimation ()
		{
			using (var layer = new CALayer ()) {
				var animation = new CABasicAnimation ();
				Assert.IsNull (layer.AnimationForKey ("key"), "#key A");
				layer.AddAnimation (animation, "key");
				Assert.IsNotNull (layer.AnimationForKey ("key"), "#key B");
			}
		}


		static int TextLayersDisposed;
		static int Generation;
		[Test]
		public void TestBug26532()
		{
			TextLayersDisposed = 0;
			Generation++;

			const int layerCount = 50;
			var thread = new Thread (() => {
				var frame = new RectangleF (0, 0, 200, 200);
				using (var layer = new CALayer ()) {
					for (int i = 0; i < layerCount; i++) {
						TextCALayer textLayer = new TextCALayer () {
							Secret = "42",
						};
						layer.AddSublayer (textLayer);
					}

					GC.Collect ();

					foreach (var slayer in layer.Sublayers.OfType<TextCALayer> ()) {
						Assert.AreEqual ("42", slayer.Secret);
					}

					foreach (var slayer in layer.Sublayers.OfType<TextCALayer> ())
						slayer.RemoveFromSuperLayer ();
				}
			});
			thread.Start ();
			thread.Join ();

			var watch = new Stopwatch ();
			watch.Start ();
			while (watch.ElapsedMilliseconds < 2000 && TextLayersDisposed < layerCount / 2) {
				GC.Collect ();
				NSRunLoop.Main.RunUntil (NSDate.Now.AddSeconds (0.05));
			}

			Assert.That (TextLayersDisposed, Is.AtLeast (layerCount / 2), "disposed text layers");
		}

		public class TextCALayer : CALayer
		{
			public string Secret;
			public int generation;

			public TextCALayer ()
			{
				generation = Generation;
			}

			protected override void Dispose (bool disposing)
			{
				if (generation == Generation) {
					TextLayersDisposed++;
				} else {
					Console.WriteLine ("TextCALayer.Dispose called for an object from a previous test run.");
				}
				base.Dispose (disposing);
			}
		}

		class Layer : CALayer { }
		class LayerDelegate : CALayerDelegate { }

		[Test]
		public void TestCALayerDelegateDispose ()
		{
			var del = new LayerDelegate ();
			var t = new Thread (() =>
			{
				var l = new Layer ();
				l.Delegate = del;
				l.Dispose ();
			})
			{
				IsBackground = true,
			};
			t.Start ();
			t.Join ();
			GC.Collect ();

			NSRunLoop.Main.RunUntil (NSDate.Now.AddSeconds (0.1));

			GC.Collect ();
			del.Dispose ();
		}
	}
}

#endif // !__WATCHOS__
