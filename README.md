# Rhino NatNet
A Rhino plug-in for [NaturalPoint](https://www.naturalpoint.com/)'s [NatNet API](http://optitrack.com/products/natnet-sdk/).

This plug-in provides real-time access to NaturalPoint's Optitrack data directly in the Rhino interface.
It provides several new commands:

- **RNNConnect**:     Attempts to connect to the NatNet server. At the moment, it is hardcoded for 'localhost'.
- **RNNGetPoints**:   Bakes the live points to the Rhino document.
- **RNNSetPlane**:    Set a transformation plane. This changes the base frame of the incoming NatNet data.
- **RNNResetPlane**:  Reset the transformation plane.
- **RNNToggleNumberDisplay**:   Toggle number labels on live points.

Once again, provided as-is. If it's useful, awesome. If you have complaints / problems / bugs / a bad day, fork it and see if you can propose a fix.

# Contact
[tsvi@kadk.dk](mailto:tsvi@kadk.dk)

[http://tomsvilans.com](http://tomsvilans.com)
