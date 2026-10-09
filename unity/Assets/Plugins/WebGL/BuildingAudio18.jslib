mergeInto(LibraryManager.library, {
 TZ18AudioPCM:function(id,ptr,count,rate){if(window.tzAudio18?.ctx)window.tzAudio18.pcm(UTF8ToString(id),HEAPF32.subarray(ptr>>2,(ptr>>2)+count),rate)},
 TZ18AudioPlay:function(id,x,y,z,level,spatial,gain,cutoff){return window.tzAudio18?.ctx?window.tzAudio18.play(UTF8ToString(id),x,y,z,level,spatial,gain,cutoff):-1},
 TZ18AudioAdjust:function(slot,gain,cutoff){window.tzAudio18?.adjust(slot,gain,cutoff)},
 TZ18AudioListener:function(x,y,z,fx,fy,fz,ux,uy,uz,volume){if(window.tzAudio18?.ctx)window.tzAudio18.listener(x,y,z,fx,fy,fz,ux,uy,uz,volume)},
 TZ18AudioClear:function(){window.tzAudio18?.clear()}
});
