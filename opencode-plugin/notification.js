import fs from 'node:fs'
import path from 'node:path'

const API_BASE = 'http://localhost:50080'
const logFilePath = path.join(
  process.env.USERPROFILE || process.env.HOME || '.',
  'Desktop',
  'opencode-notification.log',
)

let currentState = 'idle'
let lastApiCall = 0
const RATE_LIMIT_MS = 300

async function callApi(endpoint, body) {
  const now = Date.now()
  if (now - lastApiCall < RATE_LIMIT_MS) return
  lastApiCall = now

  try {
    await fetch(`${API_BASE}${endpoint}`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body),
    })
  } catch {
  }
}

async function setLight(color, mode) {
  await Promise.all([
    fetch(`${API_BASE}/color`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ color }),
    }).catch(() => {}),
    fetch(`${API_BASE}/mode`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ mode }),
    }).catch(() => {}),
  ])
}

async function stopLight() {
  await fetch(`${API_BASE}/stop`, { method: 'POST' }).catch(() => {})
}

const STATE_MAP = {
  busy:  { color: 'green', mode: 'marquee' },
  idle:  { color: 'green', mode: 'steady' },
  input: { color: 'red',   mode: 'blink' },
  error: { color: 'red',   mode: 'blink' },
}

function shouldUpdate(event) {
  const type = event?.type ?? event?.event ?? ''
  const status = event?.properties?.status?.type ?? ''
  const toolName = event?.tool ?? ''

  if (type === 'permission.updated' || type === 'permission.asked' ||
      type === 'question.asked' || type === 'question') return 'input'
  if (type === 'tool.execute.before' && toolName === 'question') return 'input'
  if (status === 'busy' || type === 'session.busy') return 'busy'
  if (type === 'session.created') return 'busy'
  if (status === 'idle' || type === 'session.idle') return 'idle'
  if (type?.includes('error') || event?.level === 'error') return 'error'
  if (type === 'session.closed') return 'none'
  return null
}

function formatEvent(event) {
  const timestamp = new Date().toISOString()
  const eventType = event?.type ?? event?.event ?? 'unknown'
  const summary = event?.summary ?? ''
  return `[${timestamp}] ${eventType}${summary ? ` - ${summary}` : ''}\n${JSON.stringify(event, null, 2)}\n\n`
}

function writeLog(text) {
  try {
    fs.appendFileSync(logFilePath, text, 'utf-8')
  } catch {
  }
}

export const NotificationPlugin = async ({ project, client, $, directory, worktree }) => {
  return {
    event: async ({ event }) => {
      const text = formatEvent(event)
      writeLog(text)

      const newState = shouldUpdate(event)
      if (newState && newState !== currentState) {
        currentState = newState
        if (newState === 'none') {
          writeLog('  → MarqueeLight: stop\n')
          await stopLight()
        } else {
          const { color, mode } = STATE_MAP[newState]
          writeLog(`  → MarqueeLight: color=${color}, mode=${mode}\n`)
          await setLight(color, mode)
        }
      }
    },
  }
}
