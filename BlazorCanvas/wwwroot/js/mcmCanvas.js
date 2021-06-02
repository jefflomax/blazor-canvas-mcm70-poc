var Mcm70JSInterop;
(function (Mcm70JSInterop) {
    // This is being run by a browser, but we don't have CommonJS
    // or other loader.  To export anything, a loader like Browserify 
    // will be required
    var Width = 932;
    // When changing method names, update JSMethod.cs
    var McmCanvas = /** @class */ (function () {
        function McmCanvas() {
            var _this = this;
            this.monoBinding = null;
            this.instance = null;
            this.initialize = function () {
                _this.monoBinding = window.BINDING;
            };
            this.startGameLoop = function (instance) {
                _this.instance = instance;
                window.requestAnimationFrame(_this.gameLoop);
                console.log("JS startGameLoop");
            };
            this.gameLoop = function (timeStamp) {
                // tameStamp is performance.now
                // float with milliseconds in int and microseconds in decimals
                console.log("JS gameLoop " + _this.counter++ + " " + timeStamp);
                if (_this.counter < 100) {
                    window.requestAnimationFrame(_this.gameLoop);
                    _this.instance.invokeMethodAsync('GameLoop', timeStamp);
                }
            };
            this.jeffGetCanvasId = function () {
                var canvasTag = document.getElementsByTagName("canvas")[0];
                return canvasTag.id;
            };
            // Draw Pixels Unmarshalled
            this.drawPixelsUnm = function (canvasId, bytes) {
                var jsCanvasId = _this.monoBinding.conv_string(canvasId);
                console.log("drawPixelsUnm " + jsCanvasId);
                //const bs = this.monoBinding.mono_array_to_js_array(bytes);
                // This apparantly does not have to copy the array
                // https://github.com/dotnet/aspnetcore/blob/e7d5306202a51949ec9e5c3f020c13fd4837099e/src/Components/Web.JS/src/Platform/Mono/MonoPlatform.ts
                var bs = window.Blazor.platform.toUint8Array(bytes);
                console.log("drawPixelsUnm bytes " + bs.length);
                var canvasElement = document.getElementById(jsCanvasId);
                var context = canvasElement.getContext('2d');
                var imageData = context.getImageData(0, 0, canvasElement.width, canvasElement.height);
                var data = imageData.data;
                for (var pix = 0; pix < bs.length; pix += 2) {
                    _this.pixON(data, bs[pix], bs[pix + 1]);
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
            };
            ///----------------------------------------------------------------------------
            //	turn the y-th cell on (0 < y < 7) in the x-th display column (0 < x < 222)
            //	SelfScan cell is implemented as 3x3 array of pixels 
            //----------------------------------------------------------------------------
            this.pixON = function (panel, x, y) {
                var x_off = 14; // x coordinate of SelfScan window with resp. to main window
                var y_off = 75; // y coordinate of SelfScan window with resp. to main window
                var i, y1;
                var x1;
                var r, g, b;
                var r1, g1, b1;
                // compute number of pixels in display from the top left corner to the top pixel of the column
                // to be displayed
                x1 = x_off + (x * 4) + 8; // x1 is the x-coordinate of the leftmost pixel of column x;
                // each column is 4-pixel wide; +8 is to get to the first column of SS
                y1 = (y_off + (y * 4)) * Width; // y1=number of pixels in panel rows from the top of panel to
                // to row y
                // was 3
                i = (x1 + y1) * 4; // i = index in panel[] of the top pixel of a column; there are 3
                // RGB values
                // colors
                r1 = 174;
                g1 = 35;
                b1 = 35; // char color -- cell corners
                r = 237;
                g = 79;
                b = 80; // char color -- cell center
                panel[i] = r1;
                panel[i + 1] = g1;
                panel[i + 2] = b1; //  xxx  -- top 3 pixels
                panel[i + 4] = r;
                panel[i + 5] = g;
                panel[i + 6] = b; //  ...
                panel[i + 8] = r1;
                panel[i + 9] = g1;
                panel[i + 10] = b1; //  ...
                i = i + (4 * Width);
                panel[i] = r;
                panel[i + 1] = g;
                panel[i + 2] = b; // xxx
                panel[i + 4] = 255;
                panel[i + 5] = 228;
                panel[i + 6] = 238; // xxx
                panel[i + 8] = r;
                panel[i + 9] = g;
                panel[i + 10] = b; // ...
                i = i + (4 * Width);
                panel[i] = r1;
                panel[i + 1] = g1;
                panel[i + 2] = b1; // xxx
                panel[i + 4] = r;
                panel[i + 5] = g;
                panel[i + 6] = b; // xxx
                panel[i + 8] = r1;
                panel[i + 9] = g1;
                panel[i + 10] = b1; // xxx
                if (y < 6) // repaint the space between pixels 
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
                    panel[i + 10] = 0x25; //= = =
                }
            };
            this.counter = 0;
        }
        return McmCanvas;
    }()); // class McmCanvas
    function Load() {
        window['mcm70'] = new McmCanvas();
    }
    Mcm70JSInterop.Load = Load;
})(Mcm70JSInterop || (Mcm70JSInterop = {})); // namespace Mcm70JSInterop
Mcm70JSInterop.Load();
//# sourceMappingURL=mcmCanvas.js.map