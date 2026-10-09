import rules from '../dist/world.js';
import classes from '../dist/classes.js';
const { W, distance } = rules;

// Round-local PVE and replenishment. All movement, hits and rewards run on the server.
export class Encounters {
  constructor(room) { this.room = room; this.npcs = new Map(); this.serial = 0; this.nextSpawn = 8; }

  notice(p, text, kind = 'supply') {
    p.rewardEvent = { seq: (p.rewardEvent?.seq || 0) + 1, at: this.room.time, text, kind };
  }

  refill(p, magazines = 2) {
    for (const gun of [p.pistol, p.primary]) {
      if (!gun || W[gun.type].limited) continue;
      gun.reserve = Math.min(W[gun.type].reserve * 2, gun.reserve + W[gun.type].mag * magazines);
    }
  }

  killReward(p, victim) {
    this.refill(p, 2);
    p.combatEvent = { seq: (p.combatEvent?.seq || 0) + 1, at: this.room.time,
      text: `击杀确认 · ${victim.name}`, kind: 'kill' };
    this.notice(p, '击杀补给 · 常规武器备弹 +2 弹匣');
  }

  drop(p, kind) {
    const names = { ammo: '弹药箱 · 常规武器 +3 弹匣', med: '急救针 · 恢复 50 HP',
      armor: '护甲核心 · 50 护甲 / 15 秒', cooldown: '战术芯片 · 技能冷却 -12 秒' };
    this.room.loot.push({ id: `special-${++this.serial}`, x: p.x, y: p.y,
      type: 'container', kind: 'special', special: kind, name: names[kind],
      taken: false, expiresAt: this.room.time + 45 });
  }

  collect(p, item) {
    if (item.special === 'med') p.hp = Math.min(p.maxHp, p.hp + 50);
    else if (item.special === 'armor') { p.armor = Math.max(p.armorUntil > this.room.time ? p.armor : 0, 50); p.armorUntil = this.room.time + 15; }
    else if (item.special === 'cooldown') p.skillReadyAt = Math.max(this.room.time, p.skillReadyAt - 12);
    else this.refill(p, 3);
    this.notice(p, item.name);
  }

  spawn() {
    const r = this.room;
    const others = [...r.players.values(), ...this.npcs.values()].filter(p => p.hp > 0);
    const candidates = r.world.floor.filter(p => others.every(a => distance(a, p) > 280));
    if (!candidates.length) return false;
    const point = candidates[Math.floor(Math.random() * candidates.length)];
    const n = { ...point, id: `npc-${r.round}-${++this.serial}`, name: '中立巡逻兵', npc: true,
      hp: 70, maxHp: 70, connected: true, angle: 0, moving: 0, flash: 0, reload: 0,
      slot: 'pistol', pistol: { type: 'USP', ammo: 12, reserve: 120, readyAt: r.time + 1 },
      input: { x: 0, y: 0, angle: 0, fire: false }, shieldUntil: r.time + 1.5,
      revealUntil: 0, thinkAt: 0, walkAngle: Math.random() * Math.PI * 2, wanderAt: r.time + 2 };
    classes.init(n, 'guardian', r.time);
    this.npcs.set(n.id, n);
    return true;
  }

  hurt(n, bullet, amount) {
    if (n.hp <= 0 || n.shieldUntil > this.room.time) return;
    const r = this.room, killer = r.players.get(bullet.owner);
    const damage = Math.min(n.hp, amount);
    n.hp = Math.max(0, n.hp - amount); n.revealUntil = r.time + 2;
    if (killer) killer.hitEvent = { seq: (killer.hitEvent?.seq || 0) + 1, at: r.time, damage };
    if (n.hp > 0) return;
    this.npcs.delete(n.id);
    const kind = ['ammo', 'med', 'armor', 'cooldown'][Math.floor(Math.random() * 4)];
    this.drop(n, kind);
    if (killer) {
      killer.npcKills = (killer.npcKills || 0) + 1;
      killer.combatEvent = { seq: (killer.combatEvent?.seq || 0) + 1, at: r.time,
        text: '巡逻兵已清除 · 特殊物资已掉落', kind: 'npc' };
    }
  }

  tick(dt) {
    const r = this.room;
    if (r.elapsed >= this.nextSpawn) {
      while (this.npcs.size < r.settings.npcCount) if (!this.spawn()) break;
      this.nextSpawn = r.elapsed + 30;
    }
    r.loot = r.loot.filter(item => !item.expiresAt || item.expiresAt > r.time);
    for (const p of r.players.values()) {
      if (!p.connected || p.hp <= 0) continue;
      if (r.time >= (p.emergencyAt || 0)) {
        p.emergencyAt = r.time + 10;
        if (p.pistol.reserve < 24) {
          p.pistol.reserve += 12;
          this.notice(p, '应急补弹 · 手枪备弹 +12');
        }
      }
    }
    for (const n of this.npcs.values()) {
      n.flash = Math.max(0, n.flash - dt);
      if (n.reload > 0) { n.reload = Math.max(0, n.reload - dt); if (!n.reload) n.pistol.ammo = 12; }
      if (r.time >= n.thinkAt) {
        n.thinkAt = r.time + .25;
        const targets = [...r.players.values()].filter(p => p.connected && p.hp > 0 && p.shieldUntil <= r.time && distance(p, n) < 460 && r.world.visible(n, p, r.time));
        n.targetId = targets.sort((a,b) => distance(n,a)-distance(n,b))[0]?.id;
      }
      const target = r.players.get(n.targetId);
      if (target?.hp > 0 && target.shieldUntil <= r.time && distance(n,target) < 500 && r.world.los(n,target)) {
        n.angle = Math.atan2(target.y - n.y, target.x - n.x);
        n.moving = distance(n,target) > 210 ? .4 : 0;
        r.world.move(n, Math.cos(n.angle)*55*n.moving, Math.sin(n.angle)*55*n.moving, dt);
        if (n.pistol.ammo <= 0) { if (!n.reload) n.reload = 1.8; }
        else if (!n.reload && n.pistol.readyAt <= r.time) {
          n.angle += (Math.random()-.5)*.3; r.shoot(n); n.pistol.readyAt = r.time + .9;
        }
      } else {
        if (r.time >= n.wanderAt) { n.walkAngle = Math.random()*Math.PI*2; n.wanderAt = r.time + 2.5; }
        n.angle = n.walkAngle; n.moving = .5;
        r.world.move(n, Math.cos(n.angle)*38, Math.sin(n.angle)*38, dt);
      }
    }
  }
}
