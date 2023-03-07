# Features
* Allow players to set a toll on pressing a button

This plugin can be used on roleplay servers to let players pay an entry fee (for example) to certain buildings.

![Charged toll example](https://i.imgur.com/fqlg0wr.png)

# Chat commands
* `/toll` - Shows the toll that is set at a button (can be used by anyone)
* `/toll <amount>` -  Configures a toll on a button (`tollbuttons.use` permission is required)
# Permissions
* `tollbuttons.use` - This permission grants users to configure a toll on their buttons
* `tollbuttons.admin` - This permissions grants users to configure a toll on anyone's buttons
* `tollbuttons.exclude` - This permission excludes a user from paying a toll (can press buttons for free)

# Configuration
```
{
  "MaximumPrice": 0,
  "TransferTollToOwner": false
}
```

* `MaximumPrice` - The maximum price that players can set on a button - set to 0 to disable
* `TransferTollToOwner` - When set to true, any paid toll will be transferred to the owner of the button