const test = require('node:test');
const assert = require('node:assert/strict');
const { harness, code } = require('./harness.cjs');

test('slow extension loading cannot stop the first animation frame', () => {
  const h = harness({}, `
    const startupFrames = [], startupEvents = {};
    requestAnimationFrame = callback => startupFrames.push(callback);
    document.addEventListener = (name, callback) => { startupEvents[name] = callback; };
    ${code}
    // A browser may paint while the parser waits for the next external script.
    if (startupFrames.length) startupFrames.shift()(16);
  `);
  h.run('startupEvents.DOMContentLoaded?.()');
  assert.equal(h.run('startupFrames.length'), 1);
  h.run('reset(); startupFrames.shift()(32)');
  assert.equal(h.run('startupFrames.length'), 1, 'render loop must keep scheduling frames');
  assert.equal(h.run('fpsFrames'), 1, 'the game must actually render');
});

test('high refresh rates draw every frame while simulation time remains fixed', () => {
  for (const hz of [30,60,120,144,240]) {
    const h = harness();
    h.run(`reset();player.shieldUntil=999;render=()=>{};last=0;for(let i=1;i<=${hz};i++)frame(i*1000/${hz})`);
    assert.ok(Math.abs(h.run('time')-1)<.001, `${hz} Hz must simulate one second`);
  }
});
