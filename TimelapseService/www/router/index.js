import Vue from 'vue';
import VueRouter from 'vue-router';

import ClientLayout from 'appRoot/vues/ClientLayout.vue';
import PassThroughChild from 'appRoot/vues/common/PassThroughChild.vue';
import AllPage from 'appRoot/vues/all/AllPage.vue';

Vue.use(VueRouter);

export default function CreateRouter(store, basePath)
{
	const router = new VueRouter({
		mode: 'history',
		routes: [
			{
				path: basePath + '', component: ClientLayout,
				children: [
					{ path: '', redirect: 'all' },
					{ path: 'all', component: AllPage, name: 'all' }
				]
			}
		],
		$store: store
	});

	router.onError(function (error)
	{
		console.error("Error while routing", error);
		toaster.error('Routing Error', error);
	});

	router.beforeEach((to, from, next) =>
	{
		if (document)
			document.title = appContext.systemName;

		next();
	});

	return router;
}