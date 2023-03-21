const events = require('events')
const InputEvent = require('input-event');

const DEVICE_PATH = '/dev/input/event0';

const input = new InputEvent(DEVICE_PATH);
const keyboard = new InputEvent.Keyboard(input);
const keyboard2 = new InputEvent.Keyboard('/dev/input/event4')

const bus = new events.EventEmitter();

keyboard.on('keypress', async ({code}) => {
	//console.log("got: " + code)
	bus.emit('key_press', code)
});

keyboard2.on('keypress', async ({code}) => {
	bus.emit('key_press', code)
})

module.exports = {
	bus
}
