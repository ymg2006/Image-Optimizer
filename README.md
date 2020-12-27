# Image-Optimizer
<pre>
Image optimizer binary to use with <a href="https://marketplace.visualstudio.com/items?itemName=fabiospampinato.vscode-optimize-images">Optimize Images</a> or windows command prompt

Config for Optimize Images of vscode :
    "optimizeImages.app": "C:\\Path to\\IOptimizer\\IOptimizer.exe",
    "optimizeImages.appOptions": ["[filepath]" , "-b"],
    "optimizeImages.searchIgnoreFolders": [".git"],

Application arguments :
you can specify file or folder immidiately after the command like : IOptimizer.exe "src" -options
  -i, --input        Set input file or folder to compress, if input is folder you must specify with -f
  -f, --filter       Set filter to search in folder , example: -f *.png
  -r, --recursive    (Default: false) Search recursively all sub folders
  -l, --lossy        (Default: false) Convert lossy or not
  -b, --backup       (Default: false) Whether to backup original files or not
  --help             Display this help screen.
  --version          Display version information.
 </pre>
