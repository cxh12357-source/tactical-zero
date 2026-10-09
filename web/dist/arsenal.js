'use strict';
// Weapon behaviour plugs into the existing solo projectile and inventory systems.
const Arsenal={
 limited(type){return !!W[type].limited},
 explode(b,x,y){const radius=W[b.weapon].blast;Art.explosions.push({x,y,at:time,radius,weapon:b.weapon});if(Art.explosions.length>24)Art.explosions.shift();emit(x,y,quality==='smooth'?12:22,['#ffe1a3','#edaa70','#8fd8c5'],140,.45,4);sound('NOVA',.7);shake=Math.max(shake,2.5);
  const origin={x,y};for(const target of [...enemies,player]){if(target.hp<=0)continue;const d=dist(origin,target);if(d>=radius||!los(origin,target))continue;damage(target,{...b,impact:{x,y},blastRadius:radius,damage:b.damage*Math.max(.3,1-d/radius*.7),friendly:target===player?false:b.friendly,head:1},false,target.x,target.y)}
 },
 updateBullets(dt){for(let i=bullets.length-1;i>=0;i--){const b=bullets[i],speed=Math.hypot(b.vx,b.vy),step=Math.min(speed*dt,b.remaining),nx=b.x+b.vx/speed*step,ny=b.y+b.vy/speed*step,wall=cast(b.x,b.y,nx,ny),prior=b.travelled;let removed=false;
  for(let pass=0;pass<3;pass++){let nearest=wall.wall?wall.t:1+1e-6,target=null,headshot=false;const targets=b.friendly?enemies:[player];for(const a of targets){if(a.hp<=0||b.hitActors?.includes(a))continue;const c=Math.cos(a.angle),s=Math.sin(a.angle),ht=segCircle(b.x,b.y,nx,ny,a.x+c*11,a.y+s*11,7),bt=segCircle(b.x,b.y,nx,ny,a.x-c*3,a.y-s*3,12);let t=null,h=false;if(ht!==null&&(bt===null||ht<=bt)){t=ht;h=true}else if(bt!==null)t=bt;if(t!==null&&t<nearest){nearest=t;target=a;headshot=h}}
   if(!target)break;b.travelled=prior+step*nearest;const x=b.x+(nx-b.x)*nearest,y=b.y+(ny-b.y)*nearest;if(W[b.weapon]?.blast){this.explode(b,x,y);removed=true;break}damage(target,b,headshot,x,y);if(!W[b.weapon]?.pierce){removed=true;break}(b.hitActors||=[]).push(target);b.damage*=.75;b.pierceLeft=(b.pierceLeft??W[b.weapon].pierce)-1;if(b.pierceLeft<=0){removed=true;break}
  }
  if(!removed&&wall.wall){if(W[b.weapon]?.blast){const t=Math.max(0,wall.t-.001);this.explode(b,b.x+(nx-b.x)*t,b.y+(ny-b.y)*t)}else{emit(wall.x,wall.y,5,wall.wall.material===2?['#ffe59a','#e7a550']:['#c9c9aa','#b8ac85'],125,.22,3);mark(wall.x,wall.y,2,'#151b1dcc')}removed=true}
  if(!removed){b.x=nx;b.y=ny;b.remaining-=step;b.travelled=prior+step;if(b.remaining<=0){if(W[b.weapon]?.blast)this.explode(b,nx,ny);removed=true}}
  if(removed)releaseBullet(i);
 }},
 catalog(){showScreen('armory');const list=$('armoryList');list.innerHTML='';for(const [type,w]of Object.entries(W)){const card=document.createElement('article');card.className='weaponCard '+w.tier;const picture=document.createElement('canvas');picture.width=300;picture.height=110;picture.setAttribute?.('aria-label',w.label);Art.weapon(picture.getContext('2d'),type,150,53,2.5);const title=document.createElement('h3');title.textContent=w.label;const source=document.createElement('span');source.className='weaponSource';source.textContent=w.tier==='airdrop'?'空投专属':type==='USP'?'初始装备':type==='AWP'?'稀有掉落':'战场拾取';const detail=document.createElement('p');detail.textContent=w.desc;const specs=document.createElement('div');specs.className='weaponSpecs';specs.textContent=`${w.pellets>1?w.damage+' × '+w.pellets:w.damage} 伤害 · ${w.mag} 发 · ${Math.round(60/w.interval)} RPM`;card.append(source,picture,title,detail,specs);list.appendChild(card)}}
};
$('viewArmory').addEventListener('click',()=>Arsenal.catalog());$('armoryBack').addEventListener('click',()=>showScreen('home'));
