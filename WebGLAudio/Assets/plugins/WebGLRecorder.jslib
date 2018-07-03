mergeInto(LibraryManager.library,{
	AudioInit:function()
	{
		window.recorder.init(function(e){},{sampleBits:16,sampleRate:44100});
		console.log("init");
	},
	AudioStart:function()
	{
		video.start();
		console.log("start");
	},
	GetAudioData:function(index)
	{
		return data[index];
	}, 
	getLength: function () {
		video.stop();
		data = video.compress();
		var length = data.length;
		//var lengthlength = video.getData()[0].length;
		//var count = length*lengthlength;
		console.log(length);
    	return length;
  	},
	AudioStop:function()
	{
		video.stop();
		console.log("stop");
	},
	AudioClear:function()
	{
		video.clear();
		console.log("clear");
	}
});