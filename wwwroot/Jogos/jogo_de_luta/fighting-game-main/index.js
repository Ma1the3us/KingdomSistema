const canvas = document.querySelector('canvas')
const c = canvas.getContext('2d')

canvas.width = 1024
canvas.height = 576

c.fillRect(0, 0, canvas.width, canvas.height)

const gravity = 0.7

// ------------------------------------------------------------
// BACKGROUND
// ------------------------------------------------------------
const background = new Sprite({
  position: { x: 0, y: 0 },
  imageSrc: './img/background.png'
})

const shop = new Sprite({
  position: { x: 600, y: 128 },
  imageSrc: './img/shop.png',
  scale: 2.75,
  framesMax: 6
})

// ------------------------------------------------------------
// PLAYER
// ------------------------------------------------------------
const player = new Fighter({
  position: { x: 100, y: 0 },
  velocity: { x: 0, y: 0 },
  imageSrc: './img/samuraiMack/Idle.png',
  framesMax: 8,
  scale: 2.5,
  offset: { x: 215, y: 157 },
  sprites: {
    idle: { imageSrc: './img/samuraiMack/Idle.png', framesMax: 8 },
    run: { imageSrc: './img/samuraiMack/Run.png', framesMax: 8 },
    jump: { imageSrc: './img/samuraiMack/Jump.png', framesMax: 2 },
    fall: { imageSrc: './img/samuraiMack/Fall.png', framesMax: 2 },
    attack1: { imageSrc: './img/samuraiMack/Attack1.png', framesMax: 6 },
    takeHit: {
      imageSrc: './img/samuraiMack/Take Hit - white silhouette.png',
      framesMax: 4
    },
    death: { imageSrc: './img/samuraiMack/Death.png', framesMax: 6 }
  },
  attackBox: {
    offset: { x: 100, y: 50 },
    width: 160,
    height: 50
  }
})

// ------------------------------------------------------------
// ENEMY IA
// ------------------------------------------------------------
const enemy = new Fighter({
  position: { x: 700, y: 0 },
  velocity: { x: 0, y: 0 },
  color: 'blue',
  imageSrc: './img/kenji/Idle.png',
  framesMax: 4,
  scale: 2.5,
  offset: { x: 215, y: 167 },
  sprites: {
    idle: { imageSrc: './img/kenji/Idle.png', framesMax: 4 },
    run: { imageSrc: './img/kenji/Run.png', framesMax: 8 },
    jump: { imageSrc: './img/kenji/Jump.png', framesMax: 2 },
    fall: { imageSrc: './img/kenji/Fall.png', framesMax: 2 },
    attack1: { imageSrc: './img/kenji/Attack1.png', framesMax: 4 },
    takeHit: { imageSrc: './img/kenji/Take hit.png', framesMax: 3 },
    death: { imageSrc: './img/kenji/Death.png', framesMax: 7 }
  },
  attackBox: {
    offset: { x: -170, y: 50 },
    width: 170,
    height: 50
  }
})

// ------------------------------------------------------------
// CONTROLES DO PLAYER
// ------------------------------------------------------------
const keys = {
  a: { pressed: false },
  d: { pressed: false }
}

decreaseTimer()

// ------------------------------------------------------------
// IA MAIS FRACA
// ------------------------------------------------------------
function enemyAI() {
  if (enemy.dead) return

  const distance = player.position.x - enemy.position.x
  const absDist = Math.abs(distance)

  enemy.velocity.x = 0

  // anda devagar
  if (absDist > 150) {
    enemy.velocity.x = distance > 0 ? 2 : -2
    enemy.switchSprite('run')
  } else {
    enemy.switchSprite('idle')
  }

  // ataque ocasional
  if (absDist < 120 && !enemy.isAttacking) {
    if (Math.random() < 0.15) enemy.attack() // bem mais fraco
  }

  // pulo ocasional
  if (player.velocity.y < 0 && enemy.velocity.y === 0) {
    if (Math.random() < 0.06) enemy.velocity.y = -15
  }
}

// ------------------------------------------------------------
// LOOP PRINCIPAL
// ------------------------------------------------------------
function animate() {
  window.requestAnimationFrame(animate)
  c.fillStyle = 'black'
  c.fillRect(0, 0, canvas.width, canvas.height)

  background.update()
  shop.update()

  c.fillStyle = 'rgba(255,255,255,0.15)'
  c.fillRect(0, 0, canvas.width, canvas.height)

  player.update()
  enemy.update()
  enemyAI()

  player.velocity.x = 0

  // Player andando
  if (keys.a.pressed) {
    player.velocity.x = -5
    player.switchSprite('run')
  } else if (keys.d.pressed) {
    player.velocity.x = 5
    player.switchSprite('run')
  } else {
    player.switchSprite('idle')
  }

  if (player.velocity.y < 0) player.switchSprite('jump')
  else if (player.velocity.y > 0) player.switchSprite('fall')

  // ------------------------------------------------------------
  // COLISÕES
  // ------------------------------------------------------------
  if (
    rectangularCollision({ rectangle1: player, rectangle2: enemy }) &&
    player.isAttacking &&
    player.framesCurrent === 4
  ) {
    enemy.takeHit()
    player.isAttacking = false
    gsap.to('#enemyHealth', { width: enemy.health + '%' })
  }

  if (player.isAttacking && player.framesCurrent === 4)
    player.isAttacking = false

  if (
    rectangularCollision({ rectangle1: enemy, rectangle2: player }) &&
    enemy.isAttacking &&
    enemy.framesCurrent === 2
  ) {
    player.takeHit()
    enemy.isAttacking = false
    gsap.to('#playerHealth', { width: player.health + '%' })
  }

  if (enemy.isAttacking && enemy.framesCurrent === 2)
    enemy.isAttacking = false

  // ------------------------------------------------------------
  // GAME OVER
  // ------------------------------------------------------------
  if (enemy.health <= 0 || player.health <= 0) {
    determineWinner({ player, enemy, timerId })
  }
}

animate()

// ------------------------------------------------------------
// CONTROLES
// ------------------------------------------------------------
window.addEventListener('keydown', (event) => {
  if (!player.dead) {
    switch (event.key) {
      case 'd':
        keys.d.pressed = true
        break
      case 'a':
        keys.a.pressed = true
        break
      case 'w':
        player.velocity.y = -20
        break
      case ' ':
        player.attack()
        break
    }
  }
})

window.addEventListener('keyup', (event) => {
  switch (event.key) {
    case 'd':
      keys.d.pressed = false
      break
    case 'a':
      keys.a.pressed = false
      break
  }
})

// ------------------------------------------------------------
// TELA FINAL + BOTÃO REINICIAR
// ------------------------------------------------------------
function determineWinner({ player, enemy, timerId }) {
  clearTimeout(timerId)

  const display = document.querySelector('#displayText')
  const txt = document.querySelector('#gameResultText')
  const btn = document.querySelector('#restartBtn')

  display.style.display = 'flex'
  btn.style.display = 'block'

  if (player.health === enemy.health) {
    txt.textContent = 'Empate!'
  } else if (player.health > enemy.health) {
    txt.textContent = 'Player 1 Venceu!'
  } else {
    txt.textContent = 'Player 2 Venceu!'
  }
}

function resetGame() {
  // Resetar vidas
  player.health = 100
  enemy.health = 100

  // Resetar posições
  player.position.x = 100
  player.position.y = 0
  enemy.position.x = 700
  enemy.position.y = 0

  // Resetar velocidades
  player.velocity.x = 0
  player.velocity.y = 0
  enemy.velocity.x = 0
  enemy.velocity.y = 0

  // Resetar estados internos do Player
  player.dead = false
  player.isAttacking = false
  player.framesCurrent = 0
  player.framesElapsed = 0
  player.switchSprite('idle')
  player.image = player.sprites.idle.image  // ← AJUSTE FUNDAMENTAL

  // Resetar estados internos do Enemy
  enemy.dead = false
  enemy.isAttacking = false
  enemy.framesCurrent = 0
  enemy.framesElapsed = 0
  enemy.switchSprite('idle')
  enemy.image = enemy.sprites.idle.image  // ← AJUSTE FUNDAMENTAL

  // Resetar HUD
  gsap.to('#playerHealth', { width: '100%' })
  gsap.to('#enemyHealth', { width: '100%' })

  // Resetar tela final
  document.querySelector('#displayText').style.display = 'none'
  document.querySelector('#restartBtn').style.display = 'none'

  // Resetar timer
  timer = 60
  decreaseTimer()
}


// botão reiniciar
document.querySelector('#restartBtn').onclick = () => resetGame()
