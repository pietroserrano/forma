<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useData } from 'vitepress'

interface VersionEntry {
  version: string
  label: string
  url: string
}

const { site } = useData()
const versions = ref<VersionEntry[]>([])
const currentVersionUrl = ref('')

/** Derive the site root (e.g. '/forma/') from a versioned base like '/forma/v2.1/'. */
function getSiteRoot(base: string): string {
  const parts = base.split('/').filter(Boolean)
  return parts.length > 0 ? `/${parts[0]}/` : '/'
}

onMounted(async () => {
  const base = site.value.base            // e.g. '/forma/latest/'
  const siteRoot = getSiteRoot(base)      // e.g. '/forma/'

  // Detect the current versioned path from the URL (e.g. '/forma/v2.1/')
  const escapedRoot = siteRoot.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')
  const match = window.location.pathname.match(new RegExp(`^(${escapedRoot}[^/]+/)`))
  currentVersionUrl.value = match ? match[1] : base

  try {
    const res = await fetch(`${siteRoot}versions.json`)
    if (res.ok) {
      versions.value = await res.json()
    }
  } catch {
    // versions.json not available (e.g. local dev) — hide the switcher
  }
})

function onChange(event: Event) {
  const target = event.target as HTMLSelectElement
  if (target.value) {
    window.location.href = target.value
  }
}
</script>

<template>
  <select
    v-if="versions.length > 0"
    class="vp-version-switcher"
    :value="currentVersionUrl"
    @change="onChange"
    aria-label="Select documentation version"
  >
    <option v-for="v in versions" :key="v.version" :value="v.url">
      {{ v.label }}
    </option>
  </select>
</template>
