﻿using S4GFXLibrary.FileReader;
using S4GFXLibrary.GFX;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace S4GFX {
    class Program {
		static ICollectionFileReader gfxFile;
		static SndFileReader sndFile;

		static bool removeAlpha, removeShadows, onlyShadows;

		static void Main(string[] args) {
			Console.WriteLine("=Settler IV image exporter/importer=========================");
			Console.WriteLine("Place this exe in the Settler IV folder!");
			Console.WriteLine("Exported images will be saved to the \"export/gfx/%GROUP%/\" folder.");
			Console.WriteLine("Exported sounds will be saved to the \"export/snd/\" folder. Run with --snd to extract sound files.");
			Console.WriteLine("============================================================");

			if (args.Contains("--snd")) {
				// --snd argument has been passed to the exe
				sndFile = LoadSnd("Snd/0");
				if (sndFile == null)
					Console.WriteLine("Error: Cannot export sounds because 0.sil or 0.snd is not available.");
				else
					SaveAllSounds("Snd/0");
			}

			Console.WriteLine("");
			Console.WriteLine("Export all: (1), Export one group: (2), Export one single image: (3), Import one image collection (4)");
			Console.WriteLine("");

			string choice = Console.ReadLine();

			switch (choice) {
				case "1":
					AskRemoveShadowsAndAlpha();

					for (int i = 0; i < 45; i++) {
						var gfx = Load("GFX/" + i);
						if (gfx == null)
							return;
						SaveAllBitmaps("GFX/" + i);
					}
					break;

				default:
				case "2": {
					REPEAT:
					Console.WriteLine("What group would you like to export the images from?");
					string choiceGroup = Console.ReadLine();

					string path = "GFX/" + choiceGroup;
					if (!File.Exists(path + ".gfx") && !File.Exists(path + ".gh5")) {
						Console.WriteLine($"Group {path} does not exist!");
						goto REPEAT;
					}

					AskRemoveShadowsAndAlpha();
					Load(path);
					SaveAllBitmaps(path);
				}
				break;

				case "3": {
					REPEAT_SINGLE_GROUP:
					Console.WriteLine("What group would you like to export an image from?");
					string choiceGroup = Console.ReadLine();

					string path = "GFX/" + choiceGroup;
					if (!File.Exists(path + ".gfx") && !File.Exists(path + ".gh5")) {
						Console.WriteLine($"Group {path} does not exist!");
						goto REPEAT_SINGLE_GROUP;
					}
					Load(path);

					REPEAT_SINGLE_IMAGE:
					Console.WriteLine($"What number has the image you want to export? This group contains: {gfxFile.GetImageCount()} images");
					string choiceImage = Console.ReadLine();

					int image = int.Parse(choiceImage);
					if (image > gfxFile.GetImageCount()) {
						Console.WriteLine($"There is no image nr. {image}!");
						goto REPEAT_SINGLE_IMAGE;
					}

					AskRemoveShadowsAndAlpha();
					SaveToBitmap(path, image, gfxFile);
				}
				break;
				case "4": { //IMPORT
					Console.WriteLine("=IMPORT=====================================================");
					Console.WriteLine("To import files, save them to the export/gfx/%GROUP%/%id%/ folder.");
					Console.WriteLine("The image collection has to be complete for the import to work");
					Console.WriteLine("To see what image has what index, export the group with (1-3) in the main menu.");
					Console.WriteLine("Example would be: export/gfx/14/1 -> replaces the woodcutter images of the trojan");
					Console.WriteLine("");
					Console.WriteLine("The finished gfx (and, but not neccesary, gil) file will be saved next to this programm.");
					Console.WriteLine("Replace the original files in the gfx folder with the newly generated ones");
					Console.WriteLine("============================================================");
					Console.WriteLine("");

					REPEAT_SINGLE_GROUP:
					Console.WriteLine("What group would you like to import a file into?");
					string choiceGroup = Console.ReadLine();

					string path = "GFX/" + choiceGroup;
					if (!File.Exists(path + ".gfx") && !File.Exists(path + ".gh5")) {
						Console.WriteLine($"Group {path} does not exist!");
						goto REPEAT_SINGLE_GROUP;
					}
					Load(path);

					REPEAT_SINGLE_IMAGE:
					Console.WriteLine($"What number has the first image in the collection you want to import into the game files? This group contains: {gfxFile.GetImageCount()} images");
					string choiceImage = Console.ReadLine();

					int image = int.Parse(choiceImage);
					if (image > gfxFile.GetImageCount()) {
						Console.WriteLine($"There is no image nr. {image}!");
						goto REPEAT_SINGLE_IMAGE;
					}

					//AskRemoveShadowsAndAlpha();
					//SaveToBitmap(path, image, gfxFile);

					ImageData[] data = LoadFromBitmap(path, image, gfxFile);
					gfxFile.ChangeImageData(choiceGroup, image, data);
					var collection = gfxFile.GetDataBufferCollection(choiceGroup);
					collection.WriteToFiles("");
				}
				break;
				//case "5": { //Secret test key
				//	REPEAT_SINGLE_GROUP:
				//	Console.WriteLine("What group would you like to export an image from?");
				//	string choiceGroup = Console.ReadLine();

				//	string path = "GFX/" + choiceGroup;
				//	if (!File.Exists(path + ".gfx")) {
				//		Console.WriteLine($"Group {path} does not exist!");
				//		goto REPEAT_SINGLE_GROUP;
				//	}
				//	Load(path);

				//	(gfxFile ).RemoveDILDependence();

				//	REPEAT_SINGLE_IMAGE:
				//	Console.WriteLine($"What number has the image you want to export? This group contains: {gfxFile.GetImageCount()} images");
				//	string choiceImage = Console.ReadLine();

				//	int image = int.Parse(choiceImage);
				//	if (image > gfxFile.GetImageCount()) {
				//		Console.WriteLine($"There is no image nr. {image}!");
				//		goto REPEAT_SINGLE_IMAGE;
				//	}

				//	AskRemoveShadowsAndAlpha();
				//	SaveToBitmap(path, image, gfxFile);
				//}
				//break;
			}

			Console.WriteLine("");
			Console.WriteLine("======");
			Console.WriteLine("FINISHED LOADING");
			Console.WriteLine("You may close this app now...");
			Console.WriteLine("======");

			gfxFile = null;
			GC.Collect();
			Console.ReadKey();
		}

		public static void AskRemoveShadowsAndAlpha() {
			Console.WriteLine("===Mask Colors===");
			Console.WriteLine("Settler IV images contain 2 separate masks.");
			Console.WriteLine("A solid green color represents the shadow in-game");
			Console.WriteLine("A solid red color represents the transparency");
			Console.WriteLine("We can remove both colors in this step");
			Console.WriteLine("=================");
			Console.WriteLine("");
			Console.WriteLine("Remove nothing (1), only transparency/Red (2), shadows/green (3) or both (4)? Save only shadows (5)");
			Console.WriteLine("");

			string choice = Console.ReadLine();
			switch (choice) {
				case "2":
				removeAlpha = true;
				break;
				case "3":
				removeShadows = true;
				break;
				case "4":
				removeAlpha = removeShadows = true;
				break;
				case "5":
				onlyShadows = true;
				break;
			}
		}

		static public SndFileReader LoadSnd(string fileId) {
			if (!File.Exists(fileId + ".sil")) {
				return null;
			}

			if (!File.Exists(fileId + ".snd")) {
				return null;
			}

			var silBinaryReader = new BinaryReader(File.Open(fileId + ".sil", FileMode.Open), Encoding.Default, true);
			var sndBinaryReader = new BinaryReader(File.Open(fileId + ".snd", FileMode.Open), Encoding.Default, true);

			var silReader = new SilFileReader(silBinaryReader);
			sndFile = new SndFileReader(sndBinaryReader, silReader);

			silBinaryReader?.Close();
			sndBinaryReader?.Close();

			return sndFile;
		}

		static public ICollectionFileReader Load(string fileId) {
			if (File.Exists(fileId + ".gh6")) {
				return DoLoadGH(fileId);
			}

			if (!File.Exists(fileId + ".gfx")) {
				return null;
			} 

			bool pil = File.Exists(fileId + ".pil");
			bool jil = File.Exists(fileId + ".jil");

			return DoLoad(fileId, pil, jil);
		}

		static public ICollectionFileReader DoLoadGH(string fileId) {
			var gh = new BinaryReader(File.Open(fileId + ".gh5", FileMode.Open), Encoding.Default, true);
			//var gl = new BinaryReader(File.Open(fileId + ".gl5", FileMode.Open), Encoding.Default, true);

			gfxFile = new GhFileReader(gh);

			gh.Close();
			//gl.Close();

			return gfxFile;
		}

		static public ICollectionFileReader DoLoad(string fileId, bool usePli, bool useJil) {
			//Console.WriteLine($"Using .jil={useJil}");

			var gfx = new BinaryReader(File.Open(fileId + ".gfx", FileMode.Open), Encoding.Default, true);
			var gil = new BinaryReader(File.Open(fileId + ".gil", FileMode.Open), Encoding.Default, true);

			BinaryReader paletteIndex, palette, directionIndex = null, jobIndex = null;

			if (usePli) {
				paletteIndex = new BinaryReader(File.Open(fileId + ".pil", FileMode.Open), Encoding.Default, true);
				palette = new BinaryReader(File.Open(fileId + ".pa6", FileMode.Open), Encoding.Default, true);
			} else {
				paletteIndex = new BinaryReader(File.Open(fileId + ".pi4", FileMode.Open), Encoding.Default, true);
				palette = new BinaryReader(File.Open(fileId + ".p46", FileMode.Open), Encoding.Default, true);
			}

			if (useJil) {
				directionIndex = new BinaryReader(File.Open(fileId + ".dil", FileMode.Open), Encoding.Default, true);
				jobIndex = new BinaryReader(File.Open(fileId + ".jil", FileMode.Open), Encoding.Default, true);
			}

			var gfxIndexList = new GilFileReader(gil);
			var paletteIndexList = new PilFileReader(paletteIndex);
			var paletteCollection = new PaletteCollection(palette, paletteIndexList);

			DilFileReader directionIndexList = null;
			JilFileReader jobIndexList = null;

			if (useJil) {
				directionIndexList = new DilFileReader(directionIndex);
				jobIndexList = new JilFileReader(jobIndex);
			}

			gfxFile = new GfxFileReader(gfx, gfxIndexList, jobIndexList, directionIndexList, paletteCollection);

			gfx?.Close();
			gil?.Close();
			paletteIndex?.Close();
			palette?.Close();
			directionIndex?.Close();
			jobIndex?.Close();

			return gfxFile;
		}

		private static void SaveAllSounds(string path, SndFileReader file = null) {
			Console.WriteLine($"Start saving: " + path);
			file = file ?? sndFile;

			for (int i = 1; i < file.GetSoundCount(); i++) {
				try {
					Directory.CreateDirectory("export/" + path);
					File.WriteAllBytes($"export/{path}/{i}.wav", file.GetSound(i));
				} catch(Exception e) {
					Console.WriteLine(e.Message);
					Console.WriteLine(e.StackTrace);
				}
			}

			Console.WriteLine($"Saved: " + path);
		}

		private static void SaveAllBitmaps(string path, ICollectionFileReader file = null) {
			Console.WriteLine($"Start saving: " + path);
			//Parallel.For(0, gfxFile.GetImageCount(), (i) => { SaveToBitmap(path, i); });
			file = file ?? gfxFile;

			for (int i = 0; i < file.GetImageCount(); i++) {
				try {
					SaveToBitmap(path, i, file);
				} catch(Exception e) {
					Console.WriteLine(e.Message);
					Console.WriteLine(e.StackTrace);
				}
			}

			Console.WriteLine($"Saved: " + path);
		}

		private static ImageData[] LoadFromBitmap(string path, int i, ICollectionFileReader file) {
			IGfxImage image = file.GetImage(i);

			bool saveByIndex = false;//file.HasDIL;
			int jobIndex = saveByIndex ? (image as GfxImage).jobIndex : 0;

			string basePath = $"export/{path}/{(saveByIndex ? $"{jobIndex}/" : "")}";

			List<ImageData> imgs = new List<ImageData>();
			var files = Directory.GetFiles(basePath, "*.*");
			int maxlen = files.Max(x => x.Length);
			var result = files.OrderBy(x => x.PadLeft(maxlen, '0')).ToList();

			int ind = 0;
			foreach (string filePath in result) {
				Bitmap map = new Bitmap(filePath);

				ImageData data = new ImageData(map.Height, map.Width);

				//image.Width = map.Width;
				//image.Height = map.Height;

				BitmapData d = map.LockBits(new Rectangle(0, 0, map.Width, map.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);

				int size = map.Height * map.Width * 4;

				data.data = new Byte[size];

				byte[] argb = new byte[size];
				Marshal.Copy(d.Scan0, argb, 0, size);

				map.UnlockBits(d);

				int index = 0;
				for (int y = 0; y < map.Height; y++) {
					for (int x = 0; x < map.Width; x++) {
						//so LockBits stores the data as bgra, because fuck you, thats why...
						data.data[index + 0] = argb[index + 2];//g1
						data.data[index + 1] = argb[index + 1];//r2
						data.data[index + 2] = argb[index + 0];//a3
						data.data[index + 3] = argb[index + 3];//b0

						index += 4;
					}
				}

				Console.WriteLine($"{ind++}/{result.Count}");

				imgs.Add(data);
			}
			return imgs.ToArray();
		}

		private static void SaveToBitmap(string path, int i, ICollectionFileReader file) {
			bool saveByIndex = file.HasDIL;

			IGfxImage image = file.GetImage(i);
			int width = image.Width;
			int height = image.Height;
			using (DirectBitmap b = new DirectBitmap(image.Width, image.Height)) {
				ImageData data = image.GetImageData();

				int index = 0;
				for (int y = 0; y < height; y++) {
					for (int x = 0; x < width; x++) {
						int alpha = 255;

						if (index >= data.data.Length)
							break;

						byte red = data.data[index + 0];
						byte green = data.data[index + 1];
						byte blue = data.data[index + 2];

						if (red == 255 && green + blue == 0) {
							alpha = removeAlpha ? 0 : alpha;
						}
						if (green == 255 && red + blue == 0) {
							alpha = removeShadows ? 0 : alpha;
						}else if (onlyShadows) {
							red = 0;
							green = 0;
							blue = 0;
							alpha = 0;
						}

						b.SetPixel(x, y, Color.FromArgb(alpha, red, green, blue));

						index += 4;
					}
				}

				int jobIndex = saveByIndex ? (image as GfxImage).jobIndex : 0;

				string basePath = $"export/{path}/{(saveByIndex ? $"{jobIndex}/" : "")}";
				Directory.CreateDirectory(basePath);
				b.Bitmap.Save(basePath + $"{i}.png", System.Drawing.Imaging.ImageFormat.Png);
			}
		}
	}
}
