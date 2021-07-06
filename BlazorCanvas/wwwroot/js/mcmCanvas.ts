
namespace Mcm70JSInterop {
	// This is being run by a browser, but we don't have CommonJS
	// or other loader.  To export anything, a loader like Browserify
	// will be required

	// Need Mono Binding typescript definitions
	interface MonoBinding {
		conv_string(s: string): string;
		mono_array_to_js_array(bs: number[]): number[];
		js_array_to_mono_array(jsa: Uint8ClampedArray): any;
		extract_mono_obj(a: Uint8ClampedArray): any;
		js_to_mono_obj(a: Uint8ClampedArray): any;
		extract_js_obj(mobj: any): Object;
	}

	declare const window: any;

	const Width: number = 932;

	// https://www.meziantou.net/optimizing-js-interop-in-a-blazor-webassembly-application.htm
	// https://github.com/mono/mono/blob/b6ef72c244bd33623d231ff05bc3d120ad36b4e9/sdks/wasm/src/binding_support.js
	// https://www.meziantou.net/optimizing-js-interop-in-a-blazor-webassembly-application.htm
	// https://github.com/majda107/blaze-cards/blob/3f3f7069100d2d4a6451a3498490eaf45887d150/BlazeCardsCore/wwwroot/blaze-cards/cards.js#L26

	class Point {
		constructor(private x: number, private y: number) {}
		public static fromPointAndSize(pt: Point, sz: Size) {
			return new Point(pt.X + sz.W, pt.Y + sz.H);
		}
		get X() { return this.x; }
		set X(value) { this.x = value; }
		get Y() { return this.y; }
		set Y(value) { this.y = value; }
	}

	class Size {
		constructor(private w: number, private h: number) {}
		get W() { return this.w; }
		set W(value) { this.w = value; }
		get H() { return this.h; }
		set H(value) { this.h = value; }
	}

	// When changing method names, update JSMethod.cs
	class McmCanvas {
		private monoBinding: MonoBinding = null;
		private instance: any = null; // for calling .NET
		private instanceTest: any = null;

		public constructor() {
		}

		public initialize = (): void => {
			// TODO: Get the canvas ID and hold a
			// reference to the canvas element
			this.monoBinding = window.BINDING;
		}

		public startGameLoop = (instance): void => {
			this.instance = instance;
			window.requestAnimationFrame(this.gameLoop);
		}

		// Called by JS
		public gameLoop = (timeStamp) => {
			// tameStamp is performance.now
			// float with milliseconds in int and microseconds in decimals

			this.instance.invokeMethodAsync('GameLoop', timeStamp).then(r => {

				const stop = r.result < 0;
				if (stop) {
					console.log(`JS ${timeStamp} ret ${r.result}`);
				} else {
					window.requestAnimationFrame(this.gameLoop);
				}
			});
		}

		/**
		 * Draw IMG element into canvasId
		 * @param elementRef
		 * @param canvasId
		 * @param width
		 * @param height
		 */
		public drawImageToCanvas = (elementRef: any,
			canvasId: string,
			width: number,
			height: number): void => {

			//const canvasTag = <HTMLCanvasElement>document.getElementsByTagName("canvas")[0];
			const canvasElement = document.getElementById(canvasId) as HTMLCanvasElement;
			const context = canvasElement.getContext('2d');
			context.drawImage(elementRef, 0, 0, width, height);
		}

		/**
		 * Marshalls a Mono/.NET 32 bit array to Javascript without copying
		 * @param system_array
		 */
		private toInt32Array(system_array: any): Int32Array {
			const dataPtr = system_array + 12;
			const length = window.Module.HEAP32[dataPtr >> 2];
			return new Int32Array(window.Module.HEAP32.buffer, dataPtr + 4, length);
		}

		/**
		 * Refresh Self-Scan display unmarshalled
		 * @param {string} canvasId - mono string
		 * @param {Uint8Array} bytes - all memory mono array 242 byte display @0x2021
		 */
		public refreshSsUnm = (canvasId, bytes): number => {
			const y_off = 75
			const y_delta = 32;
			const jsCanvasId: string = this.monoBinding.conv_string(canvasId);
			// https://github.com/dotnet/aspnetcore/blob/e7d5306202a51949ec9e5c3f020c13fd4837099e/src/Components/Web.JS/src/Platform/Mono/MonoPlatform.ts
			const memory: Uint8Array = window.Blazor.platform.toUint8Array(bytes);

			const canvasElement = document.getElementById(jsCanvasId) as HTMLCanvasElement;
			const context = canvasElement.getContext('2d');

			const imageData = context.getImageData(0, y_off,
				canvasElement.width, y_delta);
			const data = imageData.data;

			for (let i = 0, mi = 0x2021; i < 222; i++, mi++) {
				const h = memory[mi];// get a column byte from memory (it is inverted!)
				let mask = 1;
				for (let j = 0; j < 7; j++) {
					if ((h & mask) != 0) {
						this.pixON(data, i, j);
					} else {
						this.pixOFF(data, i, j);
					}
					mask <<= 1;
				}
			}
			context.putImageData(imageData,
				0, y_off,
				14, 0,
				canvasElement.width - 28, y_delta);
			return 0;
		}

		/**
		 * Clear Self Scan Display Unmarshalled
		 * @param {string} canvasId - mono string
		 */
		public clearSsUnm = (canvasId): number => {
			const y_off = 75
			const y_delta = 32;
			const jsCanvasId: string = this.monoBinding.conv_string(canvasId);

			const canvasElement = document.getElementById(jsCanvasId) as HTMLCanvasElement;
			const context = canvasElement.getContext('2d');

			const imageData = context.getImageData(0, y_off,
				canvasElement.width, y_delta);
			const data = imageData.data;

			for (let i = 0; i < 222; i++) {
				for (let j = 0; j < 7; j++) {
					this.pixOFF(data, i, j);
				}
			}
			context.putImageData(imageData, 0, y_off);

			return 0;
		}


		///----------------------------------------------------------------------------
		//	turn the y-th cell on (0 < y < 7) in the x-th display column (0 < x < 222)
		//	SelfScan cell is implemented as 3x3 array of pixels 
		//----------------------------------------------------------------------------
		private pixON = (panel:Uint8ClampedArray, x:number, y:number) : void => {
			const x_off = 14;   // x coordinate of SelfScan window with resp. to main window
			//const y_off = 75;   // y coordinate of SelfScan window with resp. to main window
			const y_off = 0;
			let i, y1;
			let x1;
			let r: number, g: number, b: number;
			let r1: number, g1: number, b1: number;

			// TODO: Adapt all drawing routines for RGB or RGBA

			// compute number of pixels in display from the top left corner to the top pixel of the column
			// to be displayed
			x1 = x_off + (x * 4) + 8;			// x1 is the x-coordinate of the leftmost pixel of column x;
			// each column is 4-pixel wide; +8 is to get to the first column of SS
			y1 = (y_off + (y * 4)) * Width; // y1=number of pixels in panel rows from the top of panel to
			// to row y
			// was 3
			i = (x1 + y1) * 4;				// i = index in panel[] of the top pixel of a column; there are 3
			// RGB values

			// colors
			r1 = 174;
			g1 = 35;
			b1 = 35;		// char color -- cell corners
			r = 237;
			g = 79;
			b = 80;		// char color -- cell center

			panel[i] = r1;
			panel[i + 1] = g1;
			panel[i + 2] = b1;		//  xxx  -- top 3 pixels

			panel[i + 4] = r;
			panel[i + 5] = g;
			panel[i + 6] = b;		//  ...

			panel[i + 8] = r1;
			panel[i + 9] = g1;
			panel[i + 10] = b1;		//  ...

			i = i + (4 * Width);
			panel[i] = r;
			panel[i + 1] = g;
			panel[i + 2] = b;         // xxx

			panel[i + 4] = 255;
			panel[i + 5] = 228;
			panel[i + 6] = 238; // xxx

			panel[i + 8] = r;
			panel[i + 9] = g;
			panel[i + 10] = b;		// ...

			i = i + (4 * Width);
			panel[i] = r1;
			panel[i + 1] = g1;
			panel[i + 2] = b1;		// xxx

			panel[i + 4] = r;
			panel[i + 5] = g;
			panel[i + 6] = b;		// xxx

			panel[i + 8] = r1;
			panel[i + 9] = g1;
			panel[i + 10] = b1;		// xxx

			if (y < 6)  // repaint the space between pixels 
			{
				i = i + (4 * Width);
				panel[i] = 0x3D;
				panel[i + 1] = 0x25;
				panel[i + 2] = 0x25;

				panel[i + 4] = 0x41;
				panel[i + 5] = 0x24;
				panel[i + 6] = 0x26;

				panel[i + 8] = 0x40;
				panel[i + 9] = 0x23;
				panel[i + 10] = 0x25;	//= = =
			}
		}

		/*-----------------------------------------------------------------------------
			turn the y-th cell off (0 < y < 7) in the x-th display column (0 < x < 222)
			SelfScan cell is implemented as 3x3 array of pixels 
		-------------------------------------------------------------------------------*/
		private pixOFF = (panel: Uint8ClampedArray, x:number, y:number) : void => {
			const x_off = 14;   // x coordinate of SelfScan window with resp. to main window
//			const y_off = 75;   // y coordinate of SelfScan window with resp. to main window
			const y_off = 0;   // y coordinate of SelfScan window with resp. to main window
			let i, y1;
			let x1;

			x1 = x_off + (x * 4) + 8;
			y1 = (y_off + (y * 4)) * Width;
			// was 3
			i = (x1 + y1) * 4;					// i = index in panel[] of the top pixel of a column; there are 3
			// 1st row
			panel[i] = 72;
			panel[i + 1] = 43;
			panel[i + 2] = 37;		// 1st pixel

			panel[i + 4] = 75;
			panel[i + 5] = 42;
			panel[i + 6] = 37;		// 2nd pixel

			panel[i + 8] = 81;
			panel[i + 9] = 46;
			panel[i + 10] = 42;		// 3rd pixel
			//2nd row
			i = i + (4 * Width);
			panel[i] = 71;
			panel[i + 1] = 40;
			panel[i + 2] = 35;

			panel[i + 4] = 79;
			panel[i + 5] = 44;
			panel[i + 6] = 40;

			panel[i + 8] = 80;
			panel[i + 9] = 42;
			panel[i + 10] = 39;

			//3rd row  
			i = i + (4 * Width);
			panel[i] = 73;
			panel[i + 1] = 43;
			panel[i + 2] = 41;

			panel[i + 4] = 82;
			panel[i + 5] = 48;
			panel[i + 6] = 47;

			panel[i + 8] = 83;
			panel[i + 9] = 47;
			panel[i + 10] = 47;

			if (y < 6)  // repaint the space between pixels
			{
				i = i + (4 * Width);
				panel[i] = 0x3D;
				panel[i + 1] = 0x25;
				panel[i + 2] = 0x25;

				panel[i + 4] = 0x41;
				panel[i + 5] = 0x24;
				panel[i + 6] = 0x26;

				panel[i + 8] = 0x40;
				panel[i + 9] = 0x23;
				panel[i + 10] = 0x25;
			}
		}

		/**
		 * Fills a rectange on the printer with uniform color 'c'
		 * @param x
		 * @param y
		 * @param w
		 * @param h
		 * @param c
		 * @param {Uint8ClampedArray} data Canvas ImageData data
		 */
		private blankBlock = (x: number,
			y: number,
			w: number,
			h: number,
			c: number,
			data: Uint8ClampedArray ): void => {
			const PrinterWidth = 944 * 4;
			const PrinterHeight = 700;

			let s1;
			let x4 = x << 2;

			// print char
			for (let y1 = 0; y1 < h; y1++) {
				s1 = ((y + y1) * PrinterWidth) + x4;
				let jsx = s1;
				for (let x1 = 0; x1 < w; x1++) {
					data[jsx++] = c;
					data[jsx++] = c;
					data[jsx++] = c;
					jsx++;
				}
			}
		}

		// Not sure how to use an inner class as a type
		clipRect = class ClipRect {
			private xy2: Point;
			private log: boolean;

			get X() { return this.xy1.X; }
			get Y() { return this.xy1.Y; }
			get W() { return this.xy2.X - this.xy1.X; }
			get H() { return this.xy2.Y - this.xy1.Y; }

			constructor(private xy1: Point, wh:Size) {
				this.xy2 = Point.fromPointAndSize(xy1, wh);
				this.log = false;
			}

			public unionXYWH(x: number, y: number, w: number, h: number): void {
				if (x < this.xy1.X) {
					this.xy1.X = x;
					this.log = true;
				}
				if (y < this.xy1.Y) {
					this.xy1.Y = y;
					this.log = true;
				}
				const x2 = x + w;
				const y2 = y + h;
				if (x2 > this.xy2.X) {
					this.xy2.X = x2;
					this.log = true;
				}
				if (y2 > this.xy2.Y) {
					this.xy2.Y = y2;
					this.log = true;
				}
				this.logChange();
			}

			private logChange(): void {
				if (this.log) {
					this.log = false;
					//console.log(this.toString());
				}
			}

			public toString(): string {
				return `${this.xy1.X} ${this.xy1.Y} ${this.xy2.X} ${this.xy2.Y}`;
			}

			public union(r: ClipRect): void {
				if (r.xy1.X < this.xy1.X) {
					this.xy1.X = r.xy1.X;
					this.log = true;
				}
				if (r.xy1.Y < this.xy1.Y) {
					this.xy1.Y = r.xy1.Y;
					this.log = true;
				}

				if (r.xy2.X > this.xy2.X) {
					this.xy2.X = r.xy2.X;
					this.log = true;
				}
				if (r.xy2.Y > this.xy2.Y) {
					this.xy2.Y = r.xy2.Y;
					this.log = true;
				}
				this.logChange();
			}

			public unionChar(ch: Point): void {

				this.unionXYWH(ch.X, ch.Y, 12, 12);
			}
		}

		private unPack(packed: number, unpacked: number[]): void {
			// 0:X (11 bit), 1:Y (11 bit), 2:C (8 bit)
			unpacked[2] = packed & 255;
			const packed24 = packed >>> 8;
			const packed16 = packed24 >>> 8;
			const packed8 = (packed16 >>> 8) & 0x3F;
			// combine the high 3 bits with the lower 8
			unpacked[0] = ((packed8 >>> 3) << 8) + (packed16 & 0xFF);
			unpacked[1] = ((packed8 & 0x07) << 8) + (packed24 & 0xFF);
		}

		private unpack1: number[] = [0, 0, 0];
		private unpack2: number[] = [0, 0, 0];


		/**
		 * Display API Printer Operations Unmarshalled
		 * Takes .NET byte[] of APL fonts, Int32[] of packed operations and operation count
		 * Draws characters or fill blocks
		 * @param {Uint8Array} aplFonts
		 * @param {Int32Array} packedOps
		 * @param {number} opCount
		 */
		public dspAplPrinterOperations = (aplFonts, packedOps, opCount): number => {
			// Marshall
			const fonts: Uint8Array = window.Blazor.platform.toUint8Array(aplFonts);
			const packs: Int32Array = this.toInt32Array(packedOps);
			const packCount: number = opCount;

			const canvasElement = document.getElementById("printer") as HTMLCanvasElement;
			const context = canvasElement.getContext('2d');
			const imageData = context.getImageData(0, 0, canvasElement.width, canvasElement.height);
			const data = imageData.data;

			// When we introduced the packed client side draw operation,
			// this lost the concept of a redraw list.

			let clipRect = new this.clipRect(
				new Point(canvasElement.width, canvasElement.height),
				new Size(-canvasElement.width, -canvasElement.height)
			);

			let i = 0;
			while(i < packCount){
				const pack = packs[i++];

				// Blank Block
				if (pack & 0x40000000) {
					// LONG #1
					// 01XXXYYY XXXXXXXX YYYYYYYY CCCCCCCC
					this.unPack(pack, this.unpack1);
					const char = this.unpack1[2];
					const x = this.unpack1[0];
					const y = this.unpack1[1];

					const pack1 = packs[i++];

					// LONG #2
					// 01WWWHHH WWWWWWWW HHHHHHHH --------
					this.unPack(pack1, this.unpack2);

					const w = this.unpack2[0];
					const h = this.unpack2[1];
					clipRect.unionXYWH(x, y, w, h);

					this.blankBlock(x, y, w, h, char, data);

				} else {
					// Draw char at x,y
					// 00XXXYYY XXXXXXXX YYYYYYYY CCCCCCCC
					this.unPack(pack, this.unpack1);
					clipRect.unionChar(new Point(this.unpack1[0], this.unpack1[1]));

					this.dspAplPrinter(this.unpack1, data, fonts);
				}
			}

//			console.log(`>> ${clipRect.toString()}`);

			context.putImageData(imageData,
				0, 0,
				clipRect.X, clipRect.Y,
				clipRect.W, clipRect.H);
			return 0;
		}


		private dspAplPrinter = (unpacked:number[], //0:x 1:y 2:c
			data: Uint8ClampedArray,
			fonts: Uint8Array): void => {

			const PrinterWidth = 944;
			const PrinterHeight = 700;
			const AplFontWidth = 3888;
			let fontOffset = 0;

			const x = unpacked[0];
			const y = unpacked[1];
			const char = unpacked[2];

			let s1;

			// compute starting pixel position of a char in printer's window 
			//const s = 4 * ((PrinterWidth * y) + x);

			// compute the first color value of i-th font in apl_fonts image
			const p = char * 36;	// 36 = (12 pixels of font image width )* 3 RGB values

			// print char
			for (let y1 = 0; y1 < 12; y1++) {
				s1 = 4 * ((y + y1) * PrinterWidth);//+ s;
				fontOffset = y1 * AplFontWidth;
				for (let x1 = 0; x1 < 12; x1++) {
					let jsx = (x+x1) *4;
					let csx = x1 * 3;
					//var fv = fonts[fontOffset + csx + p];
					if (fonts[fontOffset + csx + p] < data[s1 + jsx]) // the "if" guard is introduced to
					{
						data[s1 + jsx] = fonts[fontOffset + csx + p]; // allow overwriting characters
					}
					jsx++;
					csx++;
					if (fonts[fontOffset + csx + p] < data[s1 + jsx]) // the "if" guard is introduced to
					{
						data[s1 + jsx] = fonts[fontOffset + csx + p]; // allow overwriting characters
					}
					jsx++;
					csx++;
					if (fonts[fontOffset + csx + p] < data[s1 + jsx]) // the "if" guard is introduced to
					{
						data[s1 + jsx] = fonts[fontOffset + csx + p]; // allow overwriting characters
					}
				}
			}

		}

		public setDotNetInstance = (instance) => {
			this.instanceTest = instance;
			return 0;
		}


		/*
		// Tested passing printer image "memory" from .NET
		// with a list of rectangles to draw
		// However, the blanking of rectangles and redrawing the .NET code
		// did wasn't very efficient this way
		private renderCounter: number = 0;
		public renderPrinterUnm = (canvasId, csClipRect, bytes): number => {
			const PrinterWidth = 944;
			const PrinterHeight = 700;
			const jsCanvasId: string = this.monoBinding.conv_string(canvasId);
			const memory: Uint8Array = window.Blazor.platform.toUint8Array(bytes);
			const clipRect: Int32Array = this.toInt32Array(csClipRect);

			const canvasElement = document.getElementById(jsCanvasId) as HTMLCanvasElement;
			const context = canvasElement.getContext('2d');

			const imageData = context.getImageData(0, 0, canvasElement.width, canvasElement.height);
			const data = imageData.data;
			const testMe = PrinterWidth == canvasElement.width;
			const jsRowLength = PrinterWidth * 4;
			const csRowLength = PrinterWidth * 3;

			let minX: number;
			let minY: number;
			let maxX: number;
			let maxY: number;

			if (clipRect.length > 4) {
				console.log(`More than one rectangle ${clipRect.length}`);
			}

			const rectangles = clipRect.length >> 2;
			for (let rectangle = 0, offset = 0;
				rectangle < rectangles;
				rectangle++, offset += 4) {
				minX = clipRect[offset];
				minY = clipRect[offset + 1];
				maxX = clipRect[offset + 2];
				maxY = clipRect[offset + 3];

				this.blitRect(minX, maxX,
					minY, maxY,
					csRowLength, jsRowLength,
					data, memory
				);
				context.putImageData(imageData, 0, 0, minX, minY, maxX - minX, maxY - minY);
			}

			return 0;
		}

		private blitRect(minX: number, maxX: number,
			minY: number, maxY: number,
			csRowLength: number,
			jsRowLength: number,
			data: Uint8ClampedArray,
			memory: Uint8Array
		): void {
			for (let y = minY; y < maxY; y++) {
				let csRow = (y * csRowLength) + (minX * 3);
				let jsRow = (y * jsRowLength) + (minX * 4);
				for (let x = minX; x < maxX; x++) {
					if (data[jsRow] != memory[csRow] ||
						data[jsRow + 1] != memory[csRow + 1] ||
						data[jsRow + 2] != memory[csRow + 2]
					) {
						data[jsRow] = memory[csRow];
						data[jsRow + 1] = memory[csRow + 1];
						data[jsRow + 2] = memory[csRow + 2];
					}
					csRow += 3;
					jsRow += 4;
				}
			}
		}
		*/

		/*
		// Helpers when testing packed Int32s
		private elevenBit = (x: number): number[] => {
			const low8 = x & 0xFF;
			const high3 = (x >>> 8) & 0x0F;
			return [low8, high3];
		}

		private xyHigh = (xhi: number, yhi: number): number => {
			const xyhi = (xhi << 3) + yhi;
			return xyhi & 0x3F;
		}

		private encode32 = (xyhi: number, x: number, y: number, ch: number): number => {
			return xyhi << 24 |
				x << 16 |
				y << 8 |
				ch;
		}

		private logUint32 = (i: number, msg:string): void => {
			let binary = i.toString(2);
			if (binary.length != 32) {
				binary = '000000000000000000'.substr(0, 32 - binary.length) + binary;
			}
			const formatted = binary.substr(0, 8) + '-' + binary.substr(8, 8) + '-' + binary.substr(16, 8) + '-' + binary.substr(24, 8);
			console.log(`${msg} ${formatted}`);
		}
		*/


		/*
		public scrollTextTest = (canvasId:string): number => {
			const canvasElement = document.getElementById(canvasId) as HTMLCanvasElement;
			const context = canvasElement.getContext('2d');
			const imageData = context.getImageData(0, 24, canvasElement.width, canvasElement.height - 24);
			const data = imageData.data;
			context.putImageData(imageData, 0, 0);
			return 0;
		}
		*/


	}// class McmCanvas

	export function Load(): void {
		window['mcm70'] = new McmCanvas();
	}
}// namespace Mcm70JSInterop

Mcm70JSInterop.Load();