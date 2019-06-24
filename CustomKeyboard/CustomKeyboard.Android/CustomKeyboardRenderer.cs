using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Text;
using Android.Views;
using Android.Views.Animations;
using Android.Views.InputMethods;
using Android.Widget;
using CustomKeyboard;
using CustomKeyboard.Droid;
using Java.Lang;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using static Android.InputMethodServices.KeyboardView;

[assembly: ExportRenderer(typeof(EntryWithCustomKeyboard), typeof(CustomKeyboardRenderer))]
namespace CustomKeyboard.Droid
{
    public class CustomKeyboardRenderer : EntryRenderer, IOnKeyboardActionListener
    {
        private Context context;
        private EntryWithCustomKeyboard entryWithCustomKeyboard;
        private Android.InputMethodServices.KeyboardView mKeyboardView;
        private Android.InputMethodServices.Keyboard mKeyboard;
        private InputTypes inputTypeToUse;
        private bool keyPressed;

        public CustomKeyboardRenderer(Context context) : base(context)
        {
            this.context = context;
        }

        protected override void OnElementChanged(ElementChangedEventArgs<Entry> e)
        {
            base.OnElementChanged(e);

            var newCustomEntryKeyboard = e.NewElement as EntryWithCustomKeyboard;
            var oldCustomEntryKeyboard = e.OldElement as EntryWithCustomKeyboard;

            if (newCustomEntryKeyboard == null && oldCustomEntryKeyboard == null)
                return;

            if (e.NewElement != null)
            {
                this.entryWithCustomKeyboard = newCustomEntryKeyboard;
                this.CreateCustomKeyboard();

                this.inputTypeToUse = this.entryWithCustomKeyboard.Keyboard.ToInputType() | InputTypes.TextFlagNoSuggestions;

                this.EditText.FocusChange += Control_FocusChange;
                this.EditText.TextChanged += EditText_TextChanged;
                this.EditText.Click += EditText_Click;
                this.EditText.Touch += EditText_Touch;
            }
            if (e.OldElement != null)
            {
                this.EditText.FocusChange -= Control_FocusChange;
                this.EditText.TextChanged -= EditText_TextChanged;
                this.EditText.Click -= EditText_Click;
                this.EditText.Touch -= EditText_Touch;
            }
        }
        protected override void OnFocusChangeRequested(object sender, VisualElement.FocusRequestArgs e)
        {
            e.Result = true;

            if (e.Focus)
                this.Control.RequestFocus();
            else
                this.Control.ClearFocus();
        }
        private void Control_FocusChange(object sender, FocusChangeEventArgs e)
        {
            if (this.EditText.Text == null)
                this.EditText.Text = string.Empty;

            if (e.HasFocus)
            {
                this.mKeyboardView.OnKeyboardActionListener = this;
                if (this.Element.Keyboard == Keyboard.Text)
                    this.CreateCustomKeyboard();
                this.ShowKeyboardWithAnimation();
            }
            else
            {
                this.mKeyboardView.OnKeyboardActionListener = new NullListener();
                this.HideKeyboardView();
            }
        }

        private void EditText_TextChanged(object sender, Android.Text.TextChangedEventArgs e)
        {
            if (this.EditText.Text.Length != 0 && !this.keyPressed)
            {
                this.EditText.ClearFocus();
                return;
            }
        }
        private void EditText_Click(object sender, System.EventArgs e)
        {
            ShowKeyboardWithAnimation();
        }
        private void EditText_Touch(object sender, TouchEventArgs e)
        {
            this.EditText.InputType = InputTypes.Null;
            this.EditText.OnTouchEvent(e.Event);
            this.EditText.InputType = this.inputTypeToUse;
            e.Handled = true;
        }
        private void CreateCustomKeyboard()
        {
            var activity = (Activity)this.context;
            var rootView = activity.Window.DecorView.FindViewById(Android.Resource.Id.Content);
            var activityRootView = (ViewGroup)((ViewGroup)rootView).GetChildAt(0);
            this.mKeyboardView = activityRootView.FindViewById<Android.InputMethodServices.KeyboardView>(Resource.Id.customKeyboard);
            if (this.mKeyboardView == null)
            {
                this.mKeyboardView = (Android.InputMethodServices.KeyboardView)activity.LayoutInflater.Inflate(Resource.Layout.CustomKeyboard, null);
                this.mKeyboardView.Id = Resource.Id.customKeyboard;
                this.mKeyboardView.Focusable = true;
                this.mKeyboardView.FocusableInTouchMode = true;

                this.mKeyboardView.Release += (sender, e) => { };

                var layoutParams = new Android.Widget.RelativeLayout.LayoutParams(LayoutParams.MatchParent, LayoutParams.WrapContent);
                layoutParams.AddRule(LayoutRules.AlignParentBottom);
                activityRootView.AddView(this.mKeyboardView, layoutParams);
            }

            this.HideKeyboardView();
            this.mKeyboard = new Android.InputMethodServices.Keyboard(this.context, Resource.Xml.Special_Keyboard);
            this.SetCurrentKeyboard();
        }

        private void SetCurrentKeyboard()
        {
            this.mKeyboardView.Keyboard = this.mKeyboard;
        }
        private void ShowKeyboardWithAnimation()
        {
            if (this.mKeyboardView.Visibility == ViewStates.Gone)
            {
                var imm = (InputMethodManager)this.context.GetSystemService(Context.InputMethodService);
                imm.HideSoftInputFromWindow(this.EditText.WindowToken, 0);
                this.EditText.InputType = InputTypes.Null;  
                this.mKeyboardView.Enabled = true;
                this.mKeyboardView.Visibility = ViewStates.Visible;
            }
        }
        private void HideKeyboardView()
        {
            this.mKeyboardView.Visibility = ViewStates.Gone;
            this.mKeyboardView.Enabled = false;
            this.EditText.InputType = InputTypes.Null;
        }
        public void OnKey([GeneratedEnum] Keycode primaryCode, [GeneratedEnum] Keycode[] keyCodes)
        {
            if (!this.EditText.IsFocused)
                return;
            this.keyPressed = true;
            long eventTime = JavaSystem.CurrentTimeMillis();
            var ev = new KeyEvent(eventTime, eventTime, KeyEventActions.Down, primaryCode, 0, 0, 0, 0,
                                  KeyEventFlags.SoftKeyboard | KeyEventFlags.KeepTouchMode);
           
            var imm = (InputMethodManager)this.context.GetSystemService(Context.InputMethodService);
            imm.HideSoftInputFromWindow(this.EditText.WindowToken, HideSoftInputFlags.None);
            this.EditText.InputType = this.inputTypeToUse;

            switch (ev.KeyCode)
            {
                case Keycode.Enter:                   
                    if (this.EditText.HasFocus)
                    {                        
                        this.HideKeyboardView();
                        this.ClearFocus();
                        this.entryWithCustomKeyboard.EnterCommand?.Execute(null);
                    }
                    break;
            }
            this.EditText.SetSelection(this.EditText.Text.Length);
            if (this.EditText.HasFocus)
            {
                this.DispatchKeyEvent(ev);
                this.keyPressed = false;
            }
        }
        public void OnPress([GeneratedEnum] Keycode primaryCode)
        {
        }

        public void OnRelease([GeneratedEnum] Keycode primaryCode)
        {
        }

        public void OnText(ICharSequence text)
        {
        }

        public void SwipeDown()
        {
        }

        public void SwipeLeft()
        {
        }

        public void SwipeRight()
        {
        }

        public void SwipeUp()
        {
        }
        private class NullListener : Java.Lang.Object, IOnKeyboardActionListener
        {
            public void OnKey([GeneratedEnum] Keycode primaryCode, [GeneratedEnum] Keycode[] keyCodes)
            {
            }

            public void OnPress([GeneratedEnum] Keycode primaryCode)
            {
            }

            public void OnRelease([GeneratedEnum] Keycode primaryCode)
            {
            }

            public void OnText(ICharSequence text)
            {
            }

            public void SwipeDown()
            {
            }

            public void SwipeLeft()
            {
            }

            public void SwipeRight()
            {
            }

            public void SwipeUp()
            {
            }
        }
    }
}