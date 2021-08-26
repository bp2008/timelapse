export function ExecJSON(args)
{
	return fetch(appContext.appPath + 'json', {
		method: 'POST',
		headers: {
			'Accept': 'application/json',
			'Content-Type': 'application/json'
		},
		body: JSON.stringify(args)
	}).then(response => response.json()).then(data =>
	{
		if (data.success)
			return Promise.resolve(data);
		else
		{
			console.error("server json handler returned error response", args, data);
			return Promise.reject(new ApiError(data.error, data));
		}
	}).catch(err =>
	{
		return Promise.reject(err);
	});
}
export function GetText(path)
{
	return fetch(appContext.appPath + 'TimelapseAPI/' + path).then(response => response.text());
}
export function GetJson(path)
{
	return fetch(appContext.appPath + 'TimelapseAPI/' + path).then(response=>response.json());
}
export class ApiError extends Error
{
	constructor(message, data)
	{
		super(message);
		this.name = "ApiError";
		this.data = data;
	}
}