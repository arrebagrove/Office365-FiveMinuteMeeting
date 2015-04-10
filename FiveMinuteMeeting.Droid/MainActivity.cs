﻿using System;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using FiveMinuteMeeting.Shared.ViewModels;
using FiveMinuteMeeting.Shared;
using Android.Support.V4.Widget;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Android.Support.V7.App;

namespace FiveMinuteMeeting.Droid
{
  [Activity(Label = "My Team", MainLauncher = true, Icon = "@drawable/ic_launcher")]
  public class MainActivity : ActionBarActivity
  {

    private ContactsViewModel viewModel = App.ContactsViewModel;
    private SwipeRefreshLayout refresher;
    private ListView listView;

    protected async override void OnCreate(Bundle bundle)
    {
      base.OnCreate(bundle);

      SetContentView(Resource.Layout.Main);

      refresher = FindViewById<SwipeRefreshLayout>(Resource.Id.refresher);
      refresher.SetColorScheme(Resource.Color.blue);
      
      refresher.Refresh += async delegate
      {
        if(viewModel.IsBusy)
          return;

        await viewModel.GetContactsAsync();
        RunOnUiThread(() => { ((BaseAdapter)listView.Adapter).NotifyDataSetChanged(); });
      };

      viewModel.PropertyChanged += PropertyChanged;

      listView = FindViewById<ListView>(Resource.Id.list);

      listView.Adapter = new ContactAdapter(this, viewModel);

      listView.ItemLongClick += ListViewItemLongClick;
      listView.ItemClick += ListViewItemClick;

      AuthenticationHelper.AuthorizationParams = new AuthorizationParameters(this);
      
      viewModel.GetContactsAsync();
      refresher.PostDelayed(() =>
      {
        refresher.Refreshing = true;
      }, 1000);
    }

    void ListViewItemClick(object sender, AdapterView.ItemClickEventArgs e)
    {
      var contact = viewModel.Contacts[e.Position];
      var vm = new DetailsViewModel(contact);
      DetailActivity.ViewModel = vm;
      var intent = new Intent(this, typeof(DetailActivity));
      StartActivity(intent);
    }

    async void ListViewItemLongClick(object sender, AdapterView.ItemLongClickEventArgs e)
    {
      await viewModel.DeleteContact(viewModel.Contacts[e.Position]);
      RunOnUiThread(() => { ((BaseAdapter)listView.Adapter).NotifyDataSetChanged(); });
    }


    public override bool OnCreateOptionsMenu(IMenu menu)
    {
      MenuInflater.Inflate(Resource.Menu.main, menu);
      return base.OnCreateOptionsMenu(menu);
    }


    public override bool OnOptionsItemSelected(IMenuItem item)
    {
      switch (item.ItemId)
      {
        case Resource.Id.add:
          DetailActivity.ViewModel = null;
           var intent = new Intent(this, typeof(DetailActivity));
          StartActivity(intent);
          break;
      }
      return base.OnOptionsItemSelected(item);
    }

    protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
    {
      base.OnActivityResult(requestCode, resultCode, data);
      AuthenticationAgentContinuationHelper.SetAuthenticationAgentContinuationEventArgs(requestCode, resultCode, data);
    }
    

    protected async override void OnResume()
    {
      base.OnResume();

      if(viewModel.Contacts.Count == 0)
        viewModel.GetContactsAsync();
      else
        RunOnUiThread(() => { ((BaseAdapter)listView.Adapter).NotifyDataSetChanged(); });
    }

    void PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
      RunOnUiThread(() =>
      {
        switch (e.PropertyName)
        {
          case "IsBusy":
            {
              refresher.Refreshing = viewModel.IsBusy;
              ((BaseAdapter)listView.Adapter).NotifyDataSetChanged();
            }
            break;
        }
      });
    }
  }
}

