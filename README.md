# odradek-terrain-scanner
![final-result-720p-2 4x1-optimized](https://user-images.githubusercontent.com/20757517/235579677-e8cd1bdb-e36a-4796-908b-68245a20a1eb.gif)

This is a recreation of Death Stranding's Odradek Terrain Scanner - an on-command VFX that scans terrain traversal data, revealing where the player could fall on slippery surfaces, hide from enemies, or be swept away in deep water. The effect is achieved using VFX Graph and Custom Post Processing Component.

The detailed breakdown is available on [80.lv](https://80.lv/articles/recreating-death-stranding-odradek-terrain-scanner-in-unity/)

Project Configuration
-------
Unity 2022.1.8f1

Tested on GTX 1080, i7-7700K

Note
-------
There is a custom ScriptableObject called a Temporary Render Texture. It is, as its name implies, a ScriptableObject that holds data for a [temporary render texture](https://docs.unity3d.com/ScriptReference/RenderTexture.GetTemporary.html). It is similar to a [Render Texture](https://docs.unity3d.com/ScriptReference/RenderTexture.html), except the creation of its texture object is handled in code at runtime.

License
-------
Copyright (c) 2023 Sichen Liu.

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the “Software”), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
