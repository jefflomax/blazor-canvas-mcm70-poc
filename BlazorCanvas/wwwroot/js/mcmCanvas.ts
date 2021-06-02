
namespace Mcm70JSInterop {
	// This is being run by a browser, but we don't have CommonJS
	// or other loader.  To export anything, a loader like Browserify 
	// will be required

	// Need Mono Binding typescript definitions
	interface MonoBinding {
		conv_string(s: string): string;
		mono_array_to_js_array(bs: number[]): number[];
	}
	declare const window: any;
	const Width: number = 932;

	// When changing method names, update JSMethod.cs
	class McmCanvas {
		private monoBinding: MonoBinding = null;
		private instance: any = null;
		private counter: number;

		public constructor() {
			this.counter = 0;
		}

		public initialize = (): void => {
			this.monoBinding = window.BINDING;
		}

		public startGameLoop = (instance): void => {
			this.instance = instance;
			window.requestAnimationFrame(this.gameLoop);
			console.log("JS startGameLoop");
		}

		public gameLoop = (timeStamp) => {
			// tameStamp is performance.now
			// float with milliseconds in int and microseconds in decimals
			console.log(`JS gameLoop ${this.counter++} ${timeStamp}`);

			if (this.counter < 100) {
				window.requestAnimationFrame(this.gameLoop);
				this.instance.invokeMethodAsync('GameLoop', timeStamp);
			}
		}


		public jeffGetCanvasId = (): string => {
			const canvasTag = <HTMLCanvasElement>document.getElementsByTagName("canvas")[0];
			return canvasTag.id;
		}

		// Draw Pixels Unmarshalled
		public drawPixelsUnm = (canvasId, bytes): number => {
			const jsCanvasId: string = this.monoBinding.conv_string(canvasId);
			console.log(`drawPixelsUnm ${jsCanvasId}`);

			//const bs = this.monoBinding.mono_array_to_js_array(bytes);
			// This apparantly does not have to copy the array
			// https://github.com/dotnet/aspnetcore/blob/e7d5306202a51949ec9e5c3f020c13fd4837099e/src/Components/Web.JS/src/Platform/Mono/MonoPlatform.ts
			const bs:Uint8Array = window.Blazor.platform.toUint8Array(bytes);
			console.log(`drawPixelsUnm bytes ${bs.length}`);

			const canvasElement = document.getElementById(jsCanvasId) as HTMLCanvasElement;
			const context = canvasElement.getContext('2d');
			const imageData = context.getImageData(0, 0, canvasElement.width, canvasElement.height);
			const data = imageData.data;

			for (let pix = 0; pix < bs.length; pix += 2) {
				this.pixON(data, bs[pix], bs[pix + 1]);
			}

			/*
			for (let x = 0; x < 222; x++)
				for (let y = 0; y < 8; y+=2)
			{
				this.pixON(data, x, y);
				//window.mcm70.pixON(data, x, y);
				//data[y * canvasElement.width * 4 + x * 4] = 255;
			}
			*/

			context.putImageData(imageData, 0, 0);
			return 0;
		}

		///----------------------------------------------------------------------------
		//	turn the y-th cell on (0 < y < 7) in the x-th display column (0 < x < 222)
		//	SelfScan cell is implemented as 3x3 array of pixels 
		//----------------------------------------------------------------------------
		private pixON = (panel:Uint8ClampedArray, x:number, y:number) : void => {
			const x_off = 14;   // x coordinate of SelfScan window with resp. to main window
			const y_off = 75;   // y coordinate of SelfScan window with resp. to main window
			let i, y1;
			let x1;
			let r: number, g: number, b: number;
			let r1:number, g1:number, b1:number;

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

/*
		JeffTest = (canvasId) => {
		//console.log(`Hello world ${canvasId}`);
		const canvasElement = document.getElementById(canvasId);
		const context = canvasElement.getContext('2d');
		const imageData = context.getImageData(0, 0, canvasElement.width, canvasElement.height);
		//console.log(`ImageData Width ${imageData.width} Height ${imageData.height} Length ${imageData.data.length}`);
		let data = imageData.data;
		const rowBytes = imageData.width * 4;
		for (let i = 0; i < imageData.height; i++) {
			const rowStart = i * rowBytes;
			data[rowStart] = 255;
			data[rowStart + 1] = 0;
			data[rowStart + 2] = 0;

			data[rowStart + 4] = data[rowStart + 8] = 255;
			data[rowStart + 5] = data[rowStart + 9] = 0;
			data[rowStart + 6] = data[rowStart + 10] = 0;

			data[rowStart + 12] = 0;
			data[rowStart + 13] = 255;
			data[rowStart + 14] = 0;
		}
		//for (let j = 0; j < imageData.data.length; j++) {
		//    imageData.data[j] = 230;
		//}
		context.putImageData(imageData, 0, 0);
	}
*/

/*
		// https://www.meziantou.net/optimizing-js-interop-in-a-blazor-webassembly-application.htm
		// https://github.com/mono/mono/blob/b6ef72c244bd33623d231ff05bc3d120ad36b4e9/sdks/wasm/src/binding_support.js
		// https://www.meziantou.net/optimizing-js-interop-in-a-blazor-webassembly-application.htm
		// https://github.com/majda107/blaze-cards/blob/3f3f7069100d2d4a6451a3498490eaf45887d150/BlazeCardsCore/wwwroot/blaze-cards/cards.js#L26
		JeffHailMary = (canvasId) => {
		//Blazor.platform.
		//            const jsCanvasId = BINDING.conv_string(canvasId);
		const jsCanvasId = BINDING.conv_string(canvasId);
		console.log(`Jeff Hail Mary ${jsCanvasId}`);
		const canvasElement = document.getElementById(jsCanvasId);
		const context = canvasElement.getContext('2d');
		const imageData = context.getImageData(0, 0, canvasElement.width, canvasElement.height);
		window.JeffImageData = imageData;
		return BINDING.js_typed_array_to_array(imageData.data);
	}
*/

/*
	JeffHailMary2 = (canvasId) => {
		const jsCanvasId = BINDING.conv_string(canvasId);
		console.log(`Jeff Hail Mary2 ${jsCanvasId}`);
		const canvasElement = document.getElementById(jsCanvasId);
		const context = canvasElement.getContext('2d');
		context.putImageData(window.JeffImageData, 0, 0);
		window.JeffImageData = null;
		return 0;
	}
*/

	}// class McmCanvas

	export function Load(): void {
		window['mcm70'] = new McmCanvas();
	}
}// namespace Mcm70JSInterop

Mcm70JSInterop.Load();