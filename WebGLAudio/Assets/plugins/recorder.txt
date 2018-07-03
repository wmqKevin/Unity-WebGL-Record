//window.URL = window.URL || window.webkitURL;
//navigator.getUserMedia = navigator.getUserMedia || navigator.webkitGetUserMedia || navigator.mozGetUserMedia || navigator.msGetUserMedia;

//define("recorder", [], function() {
	function Record(stream, config){
		config = config || {};
	    config.sampleBits = config.sampleBits || 16;      //采样数位 8, 16
	    config.sampleRate = config.sampleRate || (8000);   //采样率(1/6 44100)
		
		this.timeOut = {//发送延迟
			maxTime : 45*1000,
			timeData : {}
		};
		this.context = new (window.webkitAudioContext || window.AudioContext)();
	  	this.audioInput = this.context.createMediaStreamSource(stream);
	  	this.createScript = this.context.createScriptProcessor || this.context.createJavaScriptNode;
		this.recorder = this.createScript.apply(this.context, [4096, 1, 1]);
		this.audioData = {
			size: 0,//录音文件长度
	        buffer: [],//录音缓存
	        inputSampleRate: this.context.sampleRate,//输入采样率
	        inputSampleBits: 16,//输入采样数位 8, 16
	        outputSampleRate: config.sampleRate,    //输出采样率
	        oututSampleBits: config.sampleBits       //输出采样数位 8, 16
		}
		
//		this.recorder.addEventListener('onaudioprocess', (e) => {
//		    this.input(e.inputBuffer.getChannelData(0));
//		});
		this.recorder.onaudioprocess = e=>{
	        this.input(e.inputBuffer.getChannelData(0));
	    }
	    return this;
	};
	Record.prototype.input = function(data){
		this.audioData.buffer.push(new Float32Array(data));
	    this.audioData.size += data.length;
	};
	Record.prototype.compress = function(){//合并压缩
		var data = new Float32Array(this.audioData.size),
			offset = 0;

		for (var i = 0; i < this.audioData.buffer.length; i++) {
		    data.set(this.audioData.buffer[i], offset);
		    offset += this.audioData.buffer[i].length;
		}
		//压缩
		let compression = parseInt(this.audioData.inputSampleRate / this.audioData.outputSampleRate),
		    length = data.length / compression,
			result = new Float32Array(length),
			index = 0,
			j = 0;
		while (index < length) {
		    result[index] = data[j];
		    j += compression;
		    index++;
		}
		return result;
	};
	Record.prototype.encodeMP3 = function(){//转换格式MP3

	};
	Record.prototype.encodeWAV = function(){//转换格式WAV
		var sampleRate = Math.min(this.audioData.inputSampleRate, this.audioData.outputSampleRate),
			sampleBits = Math.min(this.audioData.inputSampleBits, this.audioData.oututSampleBits),
			bytes = this.compress(),
			dataLength = bytes.length * (sampleBits / 8),
			buffer = new ArrayBuffer(44 + dataLength),
			data = new DataView(buffer),
			channelCount = 1,//单声道
			offset = 0,
			writeString = function (str) {
		        for (var i = 0; i < str.length; i++) {
		            data.setUint8(offset + i, str.charCodeAt(i));
		        }
		   	};
		
		// 资源交换文件标识符 
		writeString('RIFF'); offset += 4;
		// 下个地址开始到文件尾总字节数,即文件大小-8 
		data.setUint32(offset, 36 + dataLength, true); offset += 4;
		// WAV文件标志
		writeString('WAVE'); offset += 4;
		// 波形格式标志 
		writeString('fmt '); offset += 4;
		// 过滤字节,一般为 0x10 = 16 
		data.setUint32(offset, 16, true); offset += 4;
		// 格式类别 (PCM形式采样数据) 
		data.setUint16(offset, 1, true); offset += 2;
		// 通道数 
		data.setUint16(offset, channelCount, true); offset += 2;
		// 采样率,每秒样本数,表示每个通道的播放速度 
		data.setUint32(offset, sampleRate, true); offset += 4;
		// 波形数据传输率 (每秒平均字节数) 单声道×每秒数据位数×每样本数据位/8 
		data.setUint32(offset, channelCount * sampleRate * (sampleBits / 8), true); offset += 4;
		// 快数据调整数 采样一次占用字节数 单声道×每样本的数据位数/8 
		data.setUint16(offset, channelCount * (sampleBits / 8), true); offset += 2;
		// 每样本数据位数 
		data.setUint16(offset, sampleBits, true); offset += 2;
		// 数据标识符 
		writeString('data'); offset += 4;
		// 采样数据总数,即数据总大小-44 
		data.setUint32(offset, dataLength, true); offset += 4;
		// 写入采样数据 
		if (sampleBits === 8) {
		    for (var i = 0; i < bytes.length; i++, offset++) {
		        var s = Math.max(-1, Math.min(1, bytes[i]));
		        var val = s < 0 ? s * 0x8000 : s * 0x7FFF;
		        val = parseInt(255 / (65535 / (val + 32768)));
		        data.setInt8(offset, val, true);
		    }
		} else {
		    for (var i = 0; i < bytes.length; i++, offset += 2) {
		        var s = Math.max(-1, Math.min(1, bytes[i]));
		        data.setInt16(offset, s < 0 ? s * 0x8000 : s * 0x7FFF, true);
		    }
		}
		
		return new Blob([data], { type: 'audio/wav' });
	};
	Record.prototype.close = function(){//关闭并清除AudioContext
		this.context.close().then(()=>{},()=>{});
	};

	Record.prototype.start = function(){//开始**********************
		this.audioInput.connect(this.recorder);
	    this.recorder.connect(this.context.destination);
	    this.timeOut.timeData = window.setTimeout(res=>{
	    	this.stop();
	    },this.timeOut.maxTime);
	};

	Record.prototype.stop = function(){//停止********************
		clearTimeout(this.timeOut.timeData);
		this.recorder.disconnect();
	};

	Record.prototype.clear = function(){//清除********************
		this.audioData.size = 0;
		this.audioData.buffer = [];
	};

	Record.prototype.getBlob = function(){//获取音频文件
		this.stop();
		return this.encodeWAV();
	};

	Record.prototype.getData = function(){
		return this.audioData.buffer;
	}

	Record.prototype.play = function(){//生成音频地址给本地播放
		return window.URL.createObjectURL(this.getBlob());
	};
	Record.prototype.upload = function(names,opts){//ajax上传的formData数据
		var fd = new FormData();
		opts = opts || {};
	    fd.append(names, this.getBlob());
	    for(let k in opts){
	    	fd.append(k, opts[k]);
	    }
	    return fd;
	};

	var recordData = {};
	var examine = function(){
		if(!navigator.mediaDevices){
			return false;
		}
		if(~navigator.userAgent.indexOf("Chrome")){
			if(!~location.hostname.indexOf("127.0.") && !~location.hostname.indexOf("localhost")){
				if( !~location.protocol.indexOf("https") ){
					return false;
				}
			}
		}
		return navigator.mediaDevices.getUserMedia?true:false;
	}
	var init = function(callback, config){
		callback = callback || function(){};
		if (navigator.mediaDevices.getUserMedia) {
	        navigator.mediaDevices.getUserMedia({
	        	audio: true
	        }).then(stream=>{
	        	var _record = new Record(stream, config);
	        	video = _record;
	        	recordData = {
	        		_record : _record,
	        		stream : stream
	        	};
	            callback(recordData);
	            return recordData;
	        }).catch(error=>{
	        	recordData = {
	        		error : error
	        	};
	        	switch (error.code || error.name) {
	                case 'PERMISSION_DENIED':
	                case 'PermissionDeniedError':
	                    throwError('用户拒绝提供信息。');
	                    break;
	                case 'NOT_SUPPORTED_ERROR':
	                case 'NotSupportedError':
	                    throwError('浏览器不支持硬件设备。');
	                    break;
	                case 'MANDATORY_UNSATISFIED_ERROR':
	                case 'MandatoryUnsatisfiedError':
	                    throwError('无法发现指定的硬件设备。');
	                    break;
	                default:
	                    throwError('无法打开麦克风。异常信息:' + (error.code || error.name));
	                    break;
	            }
	        	callback(recordData);
	        	return recordData;
	        });
	    } else {
	        this.throwErr('当前浏览器不支持录音功能。'); return;
	    }
	}
	var close = function(stream){
		if(!examine()){//是否通过验证，允许使用关闭功能
			return false;
		}
		if(recordData.error){
			return false;
		}
		recordData._record.close();
		if(recordData.stream.getTracks){
			recordData.stream.getTracks()[0].stop();
		}
	}
	var throwError = function(message){
//		alert(message);
//		console.info(message);
//	 	throw new function () { this.toString = function () { return message; } }
	}
	var getData = function(){
		return recordData;
	}
	
	window.recorder = {
		getData : getData,
		init : init,
		examine : examine,
		close : close
	};


	/*
	return {
		getData : getData,
		init : init,
		examine : examine,
		close : close
	};*/
//});





//class record{
//	constructor : function(stream, config){
//		config = config || {};
//	    config.sampleBits = config.sampleBits || 16;      //采样数位 8, 16
//	    config.sampleRate = config.sampleRate || (8000);   //采样率(1/6 44100)
//		
//		this.timeOut = {//发送延迟
//			maxTime : 45*1000,
//			timeData : {}
//		};
//		this.context = new (window.webkitAudioContext || window.AudioContext)();
//	  	this.audioInput = this.context.createMediaStreamSource(stream);
//	  	this.createScript = this.context.createScriptProcessor || this.context.createJavaScriptNode;
//		this.recorder = this.createScript.apply(this.context, [4096, 1, 1]);
//		this.audioData = {
//			size: 0,//录音文件长度
//	        buffer: [],//录音缓存
//	        inputSampleRate: this.context.sampleRate,//输入采样率
//	        inputSampleBits: 16,//输入采样数位 8, 16
//	        outputSampleRate: config.sampleRate,    //输出采样率
//	        oututSampleBits: config.sampleBits       //输出采样数位 8, 16
//		}
//		
////		this.recorder.addEventListener('onaudioprocess', (e) => {
////		    this.input(e.inputBuffer.getChannelData(0));
////		});
//		this.recorder.onaudioprocess = e=>{
//	        this.input(e.inputBuffer.getChannelData(0));
//	    }
//      return this;
//	}
//	input : function(data) {
//      this.audioData.buffer.push(new Float32Array(data));
//      this.audioData.size += data.length;
//  }
//	compress : function(){//合并压缩
//		let data = new Float32Array(this.audioData.size),
//			offset = 0;
//		
//      for (var i = 0; i < this.audioData.buffer.length; i++) {
//          data.set(this.audioData.buffer[i], offset);
//          offset += this.audioData.buffer[i].length;
//      }
//      //压缩
//      let compression = parseInt(this.audioData.inputSampleRate / this.audioData.outputSampleRate),
//	        length = data.length / compression,
//      	result = new Float32Array(length),
//      	index = 0,
//      	j = 0;
//      while (index < length) {
//          result[index] = data[j];
//          j += compression;
//          index++;
//      }
//      return result;
//	}
//	encodeMP4 : function(){//转换格式MP3
//		
//	}
//	encodeWAV : function(){//转换格式WAV
//		var sampleRate = Math.min(this.audioData.inputSampleRate, this.audioData.outputSampleRate),
//			sampleBits = Math.min(this.audioData.inputSampleBits, this.audioData.oututSampleBits),
//			bytes = this.compress(),
//			dataLength = bytes.length * (sampleBits / 8),
//			buffer = new ArrayBuffer(44 + dataLength),
//			data = new DataView(buffer),
//			channelCount = 1,//单声道
//			offset = 0,
//			writeString = function (str) {
//	            for (var i = 0; i < str.length; i++) {
//	                data.setUint8(offset + i, str.charCodeAt(i));
//	            }
//	       	};
//
//      // 资源交换文件标识符 
//      writeString('RIFF'); offset += 4;
//      // 下个地址开始到文件尾总字节数,即文件大小-8 
//      data.setUint32(offset, 36 + dataLength, true); offset += 4;
//      // WAV文件标志
//      writeString('WAVE'); offset += 4;
//      // 波形格式标志 
//      writeString('fmt '); offset += 4;
//      // 过滤字节,一般为 0x10 = 16 
//      data.setUint32(offset, 16, true); offset += 4;
//      // 格式类别 (PCM形式采样数据) 
//      data.setUint16(offset, 1, true); offset += 2;
//      // 通道数 
//      data.setUint16(offset, channelCount, true); offset += 2;
//      // 采样率,每秒样本数,表示每个通道的播放速度 
//      data.setUint32(offset, sampleRate, true); offset += 4;
//      // 波形数据传输率 (每秒平均字节数) 单声道×每秒数据位数×每样本数据位/8 
//      data.setUint32(offset, channelCount * sampleRate * (sampleBits / 8), true); offset += 4;
//      // 快数据调整数 采样一次占用字节数 单声道×每样本的数据位数/8 
//      data.setUint16(offset, channelCount * (sampleBits / 8), true); offset += 2;
//      // 每样本数据位数 
//      data.setUint16(offset, sampleBits, true); offset += 2;
//      // 数据标识符 
//      writeString('data'); offset += 4;
//      // 采样数据总数,即数据总大小-44 
//      data.setUint32(offset, dataLength, true); offset += 4;
//      // 写入采样数据 
//      if (sampleBits === 8) {
//          for (var i = 0; i < bytes.length; i++, offset++) {
//              var s = Math.max(-1, Math.min(1, bytes[i]));
//              var val = s < 0 ? s * 0x8000 : s * 0x7FFF;
//              val = parseInt(255 / (65535 / (val + 32768)));
//              data.setInt8(offset, val, true);
//          }
//      } else {
//          for (var i = 0; i < bytes.length; i++, offset += 2) {
//              var s = Math.max(-1, Math.min(1, bytes[i]));
//              data.setInt16(offset, s < 0 ? s * 0x8000 : s * 0x7FFF, true);
//          }
//      }
//
//      return new Blob([data], { type: 'audio/wav' });
//	}
//	close : function(){//关闭并清除AudioContext
//		this.context.close().then(()=>{},()=>{});
//	}
//	start : function(){//开始**********************
//		this.audioInput.connect(this.recorder);
//      this.recorder.connect(this.context.destination);
//      this.timeOut.timeData = window.setTimeout(res=>{
//      	this.stop();
//      },this.timeOut.maxTime);
//	}
//	stop : function(){//停止********************
//		clearTimeout(this.timeOut.timeData);
//		this.recorder.disconnect();
//	}
//	clear : function(){//清除
//		this.audioData.size = 0;
//		this.audioData.buffer = [];
//	}
//	getBlob : function(){//获取音频文件
//		this.stop();
//		return this.encodeWAV();
//	}
//	play : function(){//生成音频地址给本地播放
//		return window.URL.createObjectURL(this.getBlob());
//	}
//	upload : function(names,opts){//ajax上传的formData数据
//		let fd = new FormData();
//		opts = opts || {};
//      fd.append(names, this.getBlob());
//      for(let k in opts){
//      	fd.append(k, opts[k]);
//      }
//      
//      return fd;
//	}
//};

//var recordData = {};
//var examine = function(){
//	if(!navigator.mediaDevices){
//		return false;
//	}
//	if(~navigator.userAgent.indexOf("Chrome")){
//		if(!~location.hostname.indexOf("127.0.0.1") && !~location.hostname.indexOf("localhost")){
//			if( !~location.protocol.indexOf("https") ){
//				return false;
//			}
//		}
//	}
//	return navigator.mediaDevices.getUserMedia?true:false;
//}
//let init = function(callback, config){
//	callback = callback || function(){};
//	if (navigator.mediaDevices.getUserMedia) {
//      navigator.mediaDevices.getUserMedia({
//      	audio: true
//      }).then(stream=>{
//      	let _record = new record(stream, config);
//      	recordData = {
//      		_record : _record,
//      		stream : stream
//      	};
//          callback(_record,stream);
//          return recordData;
//      }).catch(error=>{
//      	switch (error.code || error.name) {
//              case 'PERMISSION_DENIED':
//              case 'PermissionDeniedError':
//                  throwError('用户拒绝提供信息。');
//                  break;
//              case 'NOT_SUPPORTED_ERROR':
//              case 'NotSupportedError':
//                  throwError('浏览器不支持硬件设备。');
//                  break;
//              case 'MANDATORY_UNSATISFIED_ERROR':
//              case 'MandatoryUnsatisfiedError':
//                  throwError('无法发现指定的硬件设备。');
//                  break;
//              default:
//                  throwError('无法打开麦克风。异常信息:' + (error.code || error.name));
//                  break;
//          }
//      });
//  } else {
//      this.throwErr('当前浏览器不支持录音功能。'); return;
//  }
//}
//let close = function(stream){
//	if(!examine()){//是否通过验证，允许使用关闭功能
//		return false;
//	}
//	recordData._record.close();
//	if(recordData.stream.getTracks){
//		recordData.stream.getTracks()[0].stop();
//	}
//}
//let throwError = function(message){
////	alert(message);
// 	throw new function () { this.toString = function () { return message; } }
//}
//let getData = function(){
//	return recordData;
//}
//
//export default {
//	getData,
//	init,
//	examine,
//	close
//}