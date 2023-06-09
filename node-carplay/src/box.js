const stream = require('stream')
const events = require('events')
//const alsaVolume = require('alsa-volume');
const {Worker, workerData} = require('worker_threads')

const protocol = require('./protocol')
const usb = require('./usb')

const HEADER_SIZE = 16;
const HEARTBEAT_INTERVAL_MS = 2000;

const bus = new events.EventEmitter()
const videoOutputStream = new stream.PassThrough()
const audioSp1Stream = new stream.PassThrough()
const audioSp2Stream = new stream.PassThrough()
const microphoneInput = new stream.PassThrough()

let boxWidth, boxHeight, boxFps
let heartbeatInterval

let microphoneIsOn = false
let worker

const run = async () => {
	heartbeatInterval = setInterval(onHeartbeat, HEARTBEAT_INTERVAL_MS)

	while (true) {
		try {
			const header = await usb.read(16);
			const [type, length] = protocol.unpackHeader(header)
			if(type === 0 || length === 0) {
				console.log("OOPS: invalid packet")
				continue
			}
			const payload = await usb.read(length)
			handlePacket(type, payload)
		}
		catch(e) {
			console.log(e)
			usb.resetusb()
			return
		}
	}
}

const handlePacket = (type, payload) => {
	switch (type) {
		case protocol.type.VIDEO:
			videoOutputStream.write(payload)
			break

		case protocol.type.AUDIO:
			//Decodetype seems like channel, volume value gets sent for channel 1 before packet for channel2 arrives
			// console.log(payload.length, payload)

			const [audioType, volume, decodeType] = protocol.unpack('<LfL', payload);
			const data = payload.slice(12);
			//console.log("Type: " + audioType + " | volume: " + volume + " decodeType: " + decodeType)

			if (payload.length === 13) {
				const [command] = protocol.unpack('<B', data)
				console.log('> AUDIO', [decodeType, volume, audioType, command])

				switch (command) {
					case protocol.audioCommand.SIRI_START:
						console.log("siristart")
						microphoneIsOn = true
						bus.emit('audio_siri_start')
						setTimeout(() => { microphoneIsOn = false }, 10000)
						break

					case protocol.audioCommand.SIRI_STOP:
						console.log("siristop")
						microphoneIsOn = false
						bus.emit('audio_siri_stop')
						break
					case protocol.audioCommand.NAVI_START:
						console.log("Got NAVI_START")
						//alsaVolume.setVolume("output", 'Media', 130)
						worker = new Worker("./src/audiocontrol.js", {workerData: '-'})
						break
					case protocol.audioCommand.NAVI_STOP:
						//alsaVolume.setVolume("output", 'Media', 255)
						worker = new Worker("./src/audiocontrol.js", {workerData: '+'})
						console.log("Got NAVI_STOP")
						break
				}
			} else {
				switch (decodeType) {
					case 1:
						audioSp1Stream.write(data)
						break
					case 2:
						audioSp2Stream.write(data)
						break
					default:
						console.log("Stream not handled: " + decodeType)
						break
				}
			}
			break

		case protocol.type.SETUP: {
			console.debug('> SETUP', protocol.unpack('<LLLLLLL', payload))
			break;
		}

		case protocol.type.CARPLAY:
			console.debug('> CARPLAY', protocol.unpack('<L', payload), payload)
			button = protocol.unpack('<L', payload);
			button = button[0];
			console.debug(button);
			if (button === 3) {
				console.debug("Car Button on UI Pressed... exiting");
				process.exit();
			}
			break;

		case protocol.type.CONNECTION:
			console.debug('> CONNECTED', protocol.unpack('<L', payload))
			break;

		case protocol.type.PHASE:
			console.debug('> PHASE', protocol.unpack('<L', payload))
			break;

		case protocol.type.DISCONNECTED:
			console.debug('> DISCONNECTED', protocol.unpack('<L', payload))
			break;

		case protocol.type.DEVICE_NAME:
			console.debug('> DEVICE NAME', payload.toString())
			break;

		case protocol.type.DEVICE_SSID:
			console.debug('> DEVICE SSID', payload.toString())
			break;

		case protocol.type.KNOWN_DEVICES:
			console.debug('> KNOWN DEVICES (BLUETOOTH)\n', payload.toString().trim().split('\n').filter(l => l.trim().length).reduce((acc, v) => ({ ...acc, [v.substring(0, 17)]: v.substring(17) }), {}))
			break;

		case protocol.type.SOFTWARE_VERSION:
			console.debug('> SOFTWARE VERSION', payload.toString())
			break;

		default:
			console.debug('-', type, payload.toString())
	}
}

const onStarted = async () => {
	await new Promise(res => setTimeout(res, 500))
	await send(protocol.buildSetupPacket(boxWidth, boxHeight, boxFps, 5))

	run()

	await new Promise(res => setTimeout(res, 1000))
	await send(protocol.buildCarplayPacket(protocol.carplay.AUTO_CONNECT))
}

const onStopped = () => {
	clearInterval(heartbeatInterval)
	heartbeatInterval = undefined
	microphoneIsOn = false
}

const onMicrophoneData = async (data) => {
	if (!microphoneIsOn) {
		return
	}

	// console.log(data.byteLength)
	await send(protocol.buildAudioPacket(data))
}

const onHeartbeat = async () => {
	await send(protocol.buildHeartbeatPacket())
}

const send = async (packet) => {
	if (packet.byteLength > HEADER_SIZE) {
		await usb.write(packet.slice(0, HEADER_SIZE))
		await usb.write(packet.slice(HEADER_SIZE))
	} else {
		await usb.write(packet)
	}
}

module.exports = {
	start: (width, height, fps) => {
		boxWidth = width
		boxHeight = height
		boxFps = fps

		const usbBus = usb.start()

		usbBus.on('started', onStarted)
		usbBus.on('stopped', onStopped)

		microphoneInput.on('data', onMicrophoneData)
	},
	sendTouchUp: async (x, y) => {
		await send(protocol.buildTouchPacket(protocol.touch.UP, x/boxWidth*10000, y/boxHeight*10000))
	},
	sendTouchMove: async (x, y) => {
		await send(protocol.buildTouchPacket(protocol.touch.MOVE, x/boxWidth*10000, y/boxHeight*10000))
	},
	sendTouchDown: async (x, y) => {
		await send(protocol.buildTouchPacket(protocol.touch.DOWN, x/boxWidth*10000, y/boxHeight*10000))
	},
	sendButton: async (code) => {
		await send(protocol.buildCarplayPacket(code))
	},
	videoOutputStream,
	audioSp1Stream,
	audioSp2Stream,
	microphoneInput,
	bus,
	button: protocol.carplay,
}
