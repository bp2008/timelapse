<template>
	<div class="root">
		<div class="body">
			<router-view></router-view>
		</div>
	</div>
</template>

<script>


	export default {
		components: {},
		data()
		{
			return {
			};
		},
		computed:
		{
		},
		methods:
		{
		},
		created()
		{
		}
	};
</script>

<style scoped>
	.root
	{
		font-size: 16px;
		display: flex;
		flex-direction: column;
		align-items: center;
		width: 100%;
		height: 100%;
	}

	.body
	{
		width: 100%;
	}
</style>