# Unity WebGL Record
由于unitywebgl上不能用Microphone类，所以使用网页接口来实现

关键交互在于调用plugins中的WebGLRecorder.jslib
参考：https://docs.unity3d.com/Manual/webgl-interactingwithbrowserscripting.html

output中是发布的项目，发布后需要把index.html中加一行<script src="recorder.js"></script>
来引用recorder.js因为WebGLRecorder.jslib中需要用到recorder.js中的代码。

发布运行后点击init来初始化
然后点击start开始录音
点击getData会自动调用stop然后播放录音
如果需要重新录音，需要点击clear


另外本人不知道js中的float数组如何传入unity中，只能用在unity中for循环一个个赋值的笨办法
