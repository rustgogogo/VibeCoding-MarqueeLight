#!/usr/bin/env node

const fs = require('node:fs')
const path = require('node:path')

const API_BASE = process.env.MARQUEELIGHT_API_BASE || 'http://localhost:10080'
const logFilePath = path.join(
  process.env.USERPROFILE || process.env.HOME || '.',
  'Desktop',
  'codex-notification.log',
)

const STATE_MAP = {
  busy: { color: 'green', mode: 'marquee' },
  idle: { color: 'green', mode: 'steady' },
  input: { color: 'red', mode: 'blink' },
  error: { color: 'red', mode: 'blink' },
}

function writeLog(message) {
  try {
    fs.appendFileSync(logFilePath, `${new Date().toISOString()} ${message}\n`, 'utf-8')
  } catch {
  }
}

async function readStdin() {
  if (process.stdin.isTTY) return null

  let text = ''
  for await (const chunk of process.stdin) {
    text += chunk
  }

  if (!text.trim()) return null

  try {
    return JSON.parse(text)
  } catch {
    writeLog(`invalid stdin json: ${text.slice(0, 500)}`)
    return null
  }
}

async function post(endpoint, body) {
  const options = {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
  }

  if (body !== undefined) {
    options.body = JSON.stringify(body)
  }

  await fetch(`${API_BASE}${endpoint}`, options)
}

async function setLight(color, mode) {
  await Promise.all([
    post('/color', { color }),
    post('/mode', { mode }),
  ])
}

async function stopLight() {
  await post('/stop')
}

function inferState(payload) {
  const event = payload?.hook_event_name || payload?.event || payload?.type || ''
  const status = payload?.status || payload?.result?.status || ''

  if (status === 'error' || status === 'failed' || payload?.error) return 'error'
  if (event === 'PermissionRequest') return 'input'
  if (event === 'UserPromptSubmit' || event === 'PreToolUse') return 'busy'
  if (event === 'Stop') return 'idle'
  return null
}

async function main() {
  const requestedState = process.argv[2]
  const payload = await readStdin()
  const state = requestedState || inferState(payload)

  if (!state) {
    writeLog('ignored event without state')
    return
  }

  try {
    if (state === 'none' || state === 'stop') {
      await stopLight()
      writeLog('MarqueeLight: stop')
      return
    }

    const light = STATE_MAP[state]
    if (!light) {
      writeLog(`unknown state: ${state}`)
      return
    }

    await setLight(light.color, light.mode)
    writeLog(`MarqueeLight: state=${state}, color=${light.color}, mode=${light.mode}`)
  } catch (error) {
    writeLog(`api call failed: ${error?.message || error}`)
  }
}

main().catch((error) => {
  writeLog(`hook failed: ${error?.message || error}`)
})
