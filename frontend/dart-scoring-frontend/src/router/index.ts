import { createRouter, createWebHistory } from 'vue-router';

const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/',
      name: 'new-match',
      component: () => import('@/pages/NewMatchPage.vue')
    },
    {
      path: '/matches/:id/live',
      name: 'live-scoring',
      component: () => import('@/pages/LiveScoringPage.vue'),
      props: route => ({ matchId: Number(route.params.id) })
    },
    {
      path: '/matches/:id/summary',
      name: 'summary',
      component: () => import('@/pages/SummaryPage.vue'),
      props: route => ({ matchId: Number(route.params.id) })
    }
  ]
});

export default router;
