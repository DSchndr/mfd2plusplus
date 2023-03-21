const alsaVolume = require('alsa-volume');
var sleep = require('sleep');
const { Worker, workerData } = require('worker_threads')

const delta = 80
const sleeptime = 3
let vol

vol = alsaVolume.getVolume("output", "Media")
if(workerData === '+') {
	for(i = vol; i<=(vol+delta); i++) {
		alsaVolume.setVolume("output", 'Media', i)
		sleep.msleep(sleeptime)
	}
}
else {
	for(i = vol; i>=(vol-delta); i--) {
		alsaVolume.setVolume("output", 'Media', i)
		sleep.msleep(sleeptime)
	}
}

function sleep(millis) {
    return new Promise(resolve => setTimeout(resolve, millis));
}
