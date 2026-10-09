const integer = (value, fallback, min, max) => Number.isFinite(value)
  ? Math.max(min, Math.min(max, Math.round(value))) : fallback;

export function matchSettings(input, mode = 'ffa') {
  const s = input && typeof input === 'object' && !Array.isArray(input) ? input : {};
  return Object.freeze({
    maxHp: integer(s.maxHp, 100, 50, 500),
    scoreLimit: integer(s.scoreLimit, mode === 'teams' ? 15 : 7, 3, 100),
    matchSeconds: integer(s.matchSeconds, 300, 60, 1800),
    respawnSeconds: integer(s.respawnSeconds, 4, 1, 10),
    supplySeconds: integer(s.supplySeconds, 20, 10, 60),
    npcCount: integer(s.npcCount, 0, 0, 6),
    startingWeapon: ['USP', 'M4A1', 'VECTOR', 'Shotgun'].includes(s.startingWeapon) ? s.startingWeapon : 'USP',
    victory: s.victory === 'kills' ? 'kills' : 'score',
  });
}

export function rulesSummary(s) {
  return `${s.maxHp} HP · ${s.scoreLimit} ${s.victory === 'kills' ? '人头' : '积分'}获胜 · ${s.matchSeconds / 60} 分钟 · ${s.respawnSeconds} 秒复活 · 补给 ${s.supplySeconds} 秒 · 巡逻兵 ${s.npcCount} 名 · 出生 ${s.startingWeapon}`;
}
