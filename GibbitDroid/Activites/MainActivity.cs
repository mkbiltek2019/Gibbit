﻿using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.View;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using Gibbit.Core.Managers;
using Gibbit.Core.Models;
using GibbitDroid.Activites;
using GibbitDroid.Adapters;
using GibbitDroid.Helpers;
using System.Collections.Generic;

namespace GibbitDroid
{
    [Activity (Label = "Gibbit", MainLauncher = true, Icon = "@drawable/icon", Theme = "@style/Theme.AppCompat")]
	public class MainActivity : ActionBarActivity
	{
        private readonly FetchManager _fetch;
        private readonly UrlManager _url;
        private Android.Support.V7.Widget.SearchView _searchView;
        private ListView _listView;
        private RepoListAdapter _adapter;
        
        public User user;
        public Activity context;
        public List<Repo> repos;

        public static Token token;
        public static Repo repo;

        public MainActivity()
        {
            _fetch = new FetchManager();
            _url = new UrlManager();
        }

        protected override async void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            SetContentView(Resource.Layout.Main);

            Button signIn = FindViewById<Button>(Resource.Id.SignIn);
            ImageView userAvatar = FindViewById<ImageView>(Resource.Id.UserAvatar);
            Button getStarred = FindViewById<Button>(Resource.Id.GetStarred);
            TextView greeting = FindViewById<TextView>(Resource.Id.Greeting);
            _listView = FindViewById<ListView>(Resource.Id.StarredRepoList);

            token = await GetLocalStorage.GetLocalAccessToken(context);

            signIn.Click += async (sender, e) =>
            {
                var json = await _fetch.GetJson(_url.User, token);
                user = await ParseManager.Parse<User>(json);

                if (user != null)
                {
                    greeting.Text = string.Format("Welcome {0}", user.UserName);
                    var bitmap = GetImageHelper.GetImageBitmapFromUrl(string.Format("{0}", user.avatarUrl));
                    userAvatar.SetImageBitmap(bitmap);
                    getStarred.Enabled = true;
                    signIn.Visibility = ViewStates.Gone;
                }
            };

            getStarred.Click += async (sender, e) =>
            {
                var json = await _fetch.GetJson(_url.Starred(user), token);
                repos = await ParseManager.Parse<List<Repo>>(json);
                repos.ForEach(delegate(Repo starredRepo) 
                {
                    starredRepo.IsStarred = true;
                });

                _adapter = new RepoListAdapter(this, token, user, repos);
                _listView.Adapter = _adapter;
            };

            _listView.ItemClick += (sender, e) =>
            {
                var listView = sender as ListView;
                repo = repos[e.Position];
                var intent = new Intent(this, typeof(RepoActivity));
                StartActivity(intent);
            };
		}

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.RepoSearch, menu);

            var item = menu.FindItem(Resource.Id.action_search);

            var searchView = MenuItemCompat.GetActionView(item);
            _searchView = searchView.JavaCast<Android.Support.V7.Widget.SearchView>();

            _searchView.QueryTextSubmit += async (sender, e) =>
            {
                //TODO: Add pagination.
                var json = await _fetch.GetJson(_url.Search(e.Query), token);
                var searchedRepos = await ParseManager.Parse<Repos>(json);
                repos = searchedRepos.Data;
  

                _adapter = new RepoListAdapter(this, token, user, repos);
                _listView.Adapter = _adapter;

                e.Handled = true;
            };

            return true;
        }
    }
}


