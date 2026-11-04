/* 
Copyright 2026 Google LLC

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    https://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

#import <Foundation/Foundation.h>
#import <GoogleSignIn/GoogleSignIn.h>
#import <UIKit/UIKit.h>

#import "UnityInterface.h"

namespace {

const char* const kUnityGameObjectName = "GoogleSignInManager";

NSString* CreateNSString(const char* string) {
  return string ? [NSString stringWithUTF8String:string] : [NSString string];
}

void SetWrapperIdentifierOnce() {
  static dispatch_once_t onceToken;
  dispatch_once(&onceToken, ^{
    GIDSignIn.wrapperIdentifier = @"siwg_unity_plugin";
  });
  NSLog(@"[GoogleSignInBridge] Wrapper Identifier set to: %@", GIDSignIn.wrapperIdentifier);
}

}  // namespace

extern "C" {

void _IOSGoogleSignIn_Initialize(const char* iosClientId) {
  // Set first: the identifier only applies to requests made after it is set,
  // and the property is write-once (first writer wins).
  SetWrapperIdentifierOnce();

  if (iosClientId == NULL || strlen(iosClientId) == 0) {
    NSLog(@"[GoogleSignInBridge] ERROR: iOS Client ID is null or empty.");
    return;
  }
  GIDConfiguration* signInConfig =
      [[GIDConfiguration alloc] initWithClientID:CreateNSString(iosClientId)];
  GIDSignIn.sharedInstance.configuration = signInConfig;
  NSLog(@"[GoogleSignInBridge] Initialized with iOS Client ID.");
}

void _IOSGoogleSignIn_SignIn() {
  dispatch_async(dispatch_get_main_queue(), ^{
    UIViewController* rootViewController = UnityGetGLViewController();
    if (!rootViewController) {
      UnitySendMessage(kUnityGameObjectName, "HandleSignInError",
                       "Root view controller not found");
      return;
    }

    [GIDSignIn.sharedInstance
        signInWithPresentingViewController:rootViewController
                                completion:^(GIDSignInResult* _Nullable result,
                                             NSError* _Nullable error) {
      if (error) {
          UnitySendMessage(kUnityGameObjectName, "HandleSignInError",
                           error.localizedDescription.UTF8String);
        }
        return;
      }

      NSString* idToken = result.user.idToken.tokenString;
      if (idToken) {
        UnitySendMessage(kUnityGameObjectName, "HandleSignInSuccess",
                         idToken.UTF8String);
      } else {
        UnitySendMessage(kUnityGameObjectName, "HandleSignInError",
                         "IDToken not found in result");
      }
    }];
  });
}

}  // extern "C"