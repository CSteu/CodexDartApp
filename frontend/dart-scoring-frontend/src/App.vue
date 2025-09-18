<script setup lang="ts">
import { computed, onMounted, watch } from 'vue';
import { RouterLink, RouterView, useRoute, useRouter } from 'vue-router';
import { storeToRefs } from 'pinia';
import ToastContainer from '@/components/common/ToastContainer.vue';
import { useMatchStore } from '@/stores/matchStore';

const router = useRouter();
const route = useRoute();
const matchStore = useMatchStore();
const { activeMatch } = storeToRefs(matchStore);

const matchDescriptor = computed(() => {
  if (!activeMatch.value) {
    return null;
  }

  const players = Array.isArray(activeMatch.value.players) ? activeMatch.value.players : [];
  if (players.length === 0) {
    return `${activeMatch.value.mode}`;
  }

  const playerNames = players.map(player => player.displayName).join(' vs ');
  return `${activeMatch.value.mode} • ${playerNames}`;
});

onMounted(async () => {
  await matchStore.fetchPlayers();

  if (activeMatch.value && route.name === 'new-match') {
    router.replace({ name: 'live-scoring', params: { id: activeMatch.value.id } });
  }
});

watch(
  () => activeMatch.value?.id,
  newId => {
    if (!newId) {
      return;
    }

    if (route.name === 'new-match') {
      router.push({ name: 'live-scoring', params: { id: newId } });
    }
  }
);
</script>

<template>
  <div class="app-shell">
    <header class="app-header">
      <RouterLink class="brand" to="/">
        🎯 Codex Darts
      </RouterLink>
      <nav class="nav">
        <RouterLink class="nav__link" to="/">New Match</RouterLink>
        <RouterLink
          v-if="activeMatch"
          class="nav__link"
          :to="{ name: 'live-scoring', params: { id: activeMatch.id } }"
        >
          Live
        </RouterLink>
        <RouterLink
          v-if="activeMatch"
          class="nav__link"
          :to="{ name: 'summary', params: { id: activeMatch.id } }"
        >
          Summary
        </RouterLink>
      </nav>
      <div v-if="matchDescriptor" class="header-status">
        <span class="badge">Active</span>
        <span>{{ matchDescriptor }}</span>
      </div>
    </header>

    <main class="app-main">
      <RouterView />
    </main>

    <ToastContainer />
  </div>
</template>

<style scoped>
.app-shell {
  min-height: 100vh;
  display: flex;
  flex-direction: column;
}

.app-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  padding: 1.25rem 2rem;
  background: rgba(15, 23, 42, 0.6);
  border-bottom: 1px solid rgba(148, 163, 184, 0.16);
  position: sticky;
  top: 0;
  backdrop-filter: blur(12px);
  z-index: 10;
}

.brand {
  font-size: 1.25rem;
  font-weight: 700;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  color: var(--accent);
}

.nav {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.nav__link {
  padding: 0.35rem 0.75rem;
  border-radius: 999px;
  color: rgba(226, 232, 240, 0.85);
  border: 1px solid transparent;
  transition: border-color 0.2s ease, color 0.2s ease, background 0.2s ease;
}

.nav__link.router-link-active {
  border-color: var(--accent);
  color: var(--accent);
  background: var(--accent-muted);
}

.nav__link:focus-visible {
  outline: none;
  border-color: var(--accent);
  box-shadow: 0 0 0 3px rgba(52, 211, 153, 0.35);
}

.header-status {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  font-size: 0.9rem;
  color: rgba(226, 232, 240, 0.7);
}

.app-main {
  flex: 1;
  padding: 2rem 0 3rem;
}

@media (max-width: 768px) {
  .app-header {
    flex-wrap: wrap;
    justify-content: center;
    text-align: center;
  }

  .app-main {
    padding: 1.5rem 0 2.5rem;
  }
}
</style>
